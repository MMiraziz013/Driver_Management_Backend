using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Mapbox;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Clean.Application.Services.Mapbox;

public class MapboxService : IMapboxService
{
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;
    private readonly IUnitOfWork _uow;
    private const string GEOCODING_BASE_URL = "https://api.mapbox.com/geocoding/v5/mapbox.places";
    private const string DIRECTIONS_BASE_URL = "https://api.mapbox.com/directions/v5/mapbox/driving";
    private const double COORDINATE_MATCH_THRESHOLD_METERS = 100;
    
    public MapboxService(HttpClient httpClient, IConfiguration configuration, IUnitOfWork uow)
    {
        _httpClient = httpClient;
        _accessToken = configuration["Mapbox:AccessToken"] 
            ?? throw new ArgumentException("Mapbox AccessToken not configured");
        _uow = uow;
    }

    /// <summary>
    /// Calculate distance between pickup and dropoff with optional stops
    /// Returns distance in KILOMETERS
    /// </summary>
    public async Task<Response<double>> CalculateDistanceAsync(
    LocationWithCoordinates pickup, 
    LocationWithCoordinates dropoff, 
    List<LocationWithCoordinates>? stops = null)
    {
        try
        {
            // 1. Resolve Pickup Coords (Use existing or Geocode)
            var pCoord = pickup.Latitude.HasValue 
                ? (Lat: pickup.Latitude.Value, Lon: pickup.Longitude.Value)
                : (await GeocodeAddressAsync(CleanAddress(pickup.Address))).Data;

            // 2. Resolve Dropoff Coords
            var dCoord = dropoff.Latitude.HasValue 
                ? (Lat: dropoff.Latitude.Value, Lon: dropoff.Longitude.Value)
                : (await GeocodeAddressAsync(CleanAddress(dropoff.Address))).Data;

            if (pCoord == default || dCoord == default)
                return new Response<double>(HttpStatusCode.BadRequest, "Could not resolve locations");

            // 3. Build Path for Directions API
            var path = new List<(double Lon, double Lat)> { (pCoord.Lon, pCoord.Lat) };
            
            Console.WriteLine($"\n[MapboxService] CalculateDistanceAsync called:");
            Console.WriteLine($"  Pickup: {pickup.Address} ({pickup.Latitude}, {pickup.Longitude})");
            Console.WriteLine($"  Dropoff: {dropoff.Address} ({dropoff.Latitude}, {dropoff.Longitude})");
            Console.WriteLine($"  Stops count: {stops?.Count ?? 0}");
            
            if (stops != null && stops.Any())
            {
                Console.WriteLine($"Processing {stops.Count} stops...");
                foreach (var stop in stops)
                {
                    var sCoord = stop.Latitude.HasValue 
                        ? (Lat: stop.Latitude.Value, Lon: stop.Longitude.Value)
                        : (await GeocodeAddressAsync(CleanAddress(stop.Address))).Data;
                
                    if (sCoord != default)
                    {
                        path.Add((sCoord.Lon, sCoord.Lat));
                        Console.WriteLine($"  + Stop added: ({sCoord.Lat}, {sCoord.Lon})");
                    }
                    else
                    {
                        Console.WriteLine($"  ⚠️ Stop failed to geocode: {stop.Address}");
                    }
                }
            }

            path.Add((dCoord.Lon, dCoord.Lat));

            Console.WriteLine($"Total waypoints in route: {path.Count}");

            // 4. Check if it's a simple point-to-point with no stops
            // ONLY return 0 if pickup == dropoff AND there are no intermediate stops
            if (path.Count == 2 && AreCoordinatesClose(pCoord.Lat, pCoord.Lon, dCoord.Lat, dCoord.Lon))
            {
                Console.WriteLine("Same pickup/dropoff with no stops - returning 0km");
                return new Response<double>(HttpStatusCode.OK, 0.0);
            }

            // 5. Get actual road distance through all waypoints
            var distanceMeters = await GetDirectionsDistanceAsync(path);
            
            if (distanceMeters.HasValue)
            {
                var distanceKm = distanceMeters.Value / 1000.0;
                Console.WriteLine($"✓ Mapbox route distance: {distanceKm:F2}km");
                return new Response<double>(HttpStatusCode.OK, distanceKm);
            }
            else
            {
                Console.WriteLine("⚠️ Mapbox API returned no distance");
                return new Response<double>(HttpStatusCode.OK, 0.0);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Mapbox error: {ex.Message}");
            return new Response<double>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }


    /// <summary>
    /// Geocode an address to lat/lon coordinates with caching
    /// </summary>
    public async Task<Response<(double Lat, double Lon)>> GeocodeAddressAsync(string address)
    {
        try
        {
            // Check cache first
            var cached = await _uow.CachedLocations.GetByAddressAsync(address);
            
            if (cached != null)
            {
                Console.WriteLine($"[Mapbox] ✓ Cache hit for '{address}': ({cached.Latitude}, {cached.Longitude})");
                return new Response<(double, double)>(HttpStatusCode.OK, (cached.Latitude, cached.Longitude));
            }
            
            Console.WriteLine($"[Mapbox] Geocoding '{address}' via Mapbox API");
            
            // Call Mapbox Geocoding API
            var searchQuery = Uri.EscapeDataString($"{address}, Tashkent, Uzbekistan");
            var url = $"{GEOCODING_BASE_URL}/{searchQuery}.json?access_token={_accessToken}&limit=1";
            
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Mapbox] ❌ Geocoding API failed with status: {response.StatusCode}");
                return new Response<(double, double)>(HttpStatusCode.BadRequest, 
                    "Geocoding API request failed");
            }
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GeocodeResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (result?.Features == null || !result.Features.Any())
            {
                Console.WriteLine($"[Mapbox] ⚠️ No coordinates found for: {address}");
                return new Response<(double, double)>(HttpStatusCode.NotFound, 
                    $"No coordinates found for address: {address}");
            }
            
            var feature = result.Features.First();
            var lon = feature.Center[0];
            var lat = feature.Center[1];
            
            Console.WriteLine($"[Mapbox] ✓ Geocoded '{address}' -> ({lat}, {lon}) - {feature.PlaceName}");
            
            // Cache the result
            await _uow.CachedLocations.AddAsync(new CachedLocation
            {
                AddressName = address,
                Longitude = lon,
                Latitude = lat,
                CachedAt = DateTime.UtcNow
            });
            await _uow.CompleteAsync();
            
            return new Response<(double, double)>(HttpStatusCode.OK, (lat, lon));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mapbox] ❌ Geocoding error: {ex.Message}");
            return new Response<(double, double)>(HttpStatusCode.InternalServerError, 
                new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// Get driving distance from Mapbox Directions API
    /// </summary>
    private async Task<double?> GetDirectionsDistanceAsync(List<(double Lon, double Lat)> coordinates)
    {
        try
        {
            if (coordinates.Count < 2)
            {
                Console.WriteLine($"[Mapbox] ⚠️ Need at least 2 coordinates for directions");
                return null;
            }
            
            var coordString = string.Join(";", coordinates.Select(c => $"{c.Lon},{c.Lat}"));
            var url = $"{DIRECTIONS_BASE_URL}/{coordString}?access_token={_accessToken}&geometries=geojson&overview=full";
            
            Console.WriteLine($"[Mapbox] Calling Directions API with {coordinates.Count} waypoints");
            
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Mapbox] ❌ Directions API failed with status: {response.StatusCode}");
                return null;
            }
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DirectionsResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (result?.Routes == null || !result.Routes.Any())
            {
                Console.WriteLine($"[Mapbox] ⚠️ No routes found");
                return null;
            }
            
            var distance = result.Routes.First().Distance;
            Console.WriteLine($"[Mapbox] ✓ Route distance: {distance:F2} meters ({distance / 1000.0:F2} km)");
            
            return distance;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mapbox] ❌ Directions error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Clean address string - extract location name from routing details
    /// Example: "PU: Hyatt Regency, Terminal A" -> "Hyatt Regency"
    /// </summary>
    private string CleanAddress(string rawAddress)
    {
        if (string.IsNullOrWhiteSpace(rawAddress))
            return string.Empty;
        
        // Remove prefixes like "PU:", "DO:", "ST:"
        var cleaned = Regex.Replace(rawAddress, @"^(PU|DO|ST):\s*", "", RegexOptions.IgnoreCase);
        
        // Take first segment before comma (main location)
        var parts = cleaned.Split(',');
        var mainLocation = parts[0].Trim();
        
        return mainLocation;
    }

    /// <summary>
    /// Check if two coordinates are within threshold distance
    /// </summary>
    private bool AreCoordinatesClose(double lat1, double lon1, double lat2, double lon2)
    {
        var distance = CalculateHaversineDistance(lat1, lon1, lat2, lon2);
        return distance <= COORDINATE_MATCH_THRESHOLD_METERS;
    }

    /// <summary>
    /// Calculate distance between two coordinates using Haversine formula
    /// Returns distance in meters
    /// </summary>
    private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth's radius in meters
        
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return R * c;
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}