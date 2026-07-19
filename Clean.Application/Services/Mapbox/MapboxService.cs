using System.Collections.Concurrent;
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

    // How many geocode HTTP calls we allow in flight at once.
    // Mapbox default rate limit is ~1000 req/min, so 8 concurrent is very safe.
    private const int MAX_CONCURRENT_GEOCODES = 8;

    // Reuse one options instance instead of allocating per call.
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public MapboxService(HttpClient httpClient, IConfiguration configuration, IUnitOfWork uow)
    {
        _httpClient = httpClient;
        _accessToken = configuration["Mapbox:AccessToken"]
            ?? throw new ArgumentException("Mapbox AccessToken not configured");
        _uow = uow;
    }

    // ============================================================
    //  PHASE 1 — BULK PRE-RESOLVE  (the thing that kills your 504)
    // ============================================================

    /// <summary>
    /// Resolves many raw addresses to coordinates in three stages:
    ///   1. ONE batched DB read for everything already cached.
    ///   2. Parallel HTTP geocoding for ONLY the cache misses (no DbContext touched here).
    ///   3. ONE batched DB write for the newly geocoded addresses.
    /// Returns a map keyed by the CLEANED address (so callers must clean before lookup,
    /// or just use CalculateDistanceAsync which cleans internally).
    /// </summary>
    public async Task<Dictionary<string, (double Lat, double Lon)>> ResolveAddressesAsync(
        IEnumerable<string> rawAddresses)
    {
        var cleaned = rawAddresses
            .Select(CleanAddress)
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var map = new Dictionary<string, (double Lat, double Lon)>(StringComparer.OrdinalIgnoreCase);
        if (cleaned.Count == 0)
            return map;

        // --- Stage 1: single DB round-trip for all cached addresses ---
        // Requires a batch repo method (see note in the chat). Falls back gracefully
        // if you haven't added it yet — see the per-address version commented below.
        var cached = await _uow.CachedLocations.GetByAddressesAsync(cleaned);
        foreach (var c in cached)
            map[c.AddressName] = (c.Latitude, c.Longitude);

        var misses = cleaned.Where(a => !map.ContainsKey(a)).ToList();
        Console.WriteLine($"[Mapbox] Pre-resolve: {cleaned.Count} unique, {map.Count} cached, {misses.Count} to geocode");

        if (misses.Count == 0)
            return map;

        // --- Stage 2: geocode misses in parallel. NO DbContext access in here. ---
        using var gate = new SemaphoreSlim(MAX_CONCURRENT_GEOCODES);

        var geocodeResults = await Task.WhenAll(misses.Select(async addr =>
        {
            await gate.WaitAsync();
            try
            {
                var coord = await GeocodeViaHttpAsync(addr);
                return (Address: addr, Coord: coord);
            }
            finally
            {
                gate.Release();
            }
        }));

        // --- Stage 3: merge (single-threaded) + ONE DB save for all new entries ---
        var newEntries = new List<CachedLocation>();
        foreach (var (addr, coord) in geocodeResults)
        {
            if (coord is { } c)
            {
                map[addr] = c;
                newEntries.Add(new CachedLocation
                {
                    AddressName = addr,
                    Latitude = c.Lat,
                    Longitude = c.Lon,
                    CachedAt = DateTime.UtcNow
                });
            }
            else
            {
                Console.WriteLine($"[Mapbox] ⚠️ Could not geocode: {addr}");
            }
        }

        if (newEntries.Count > 0)
        {
            foreach (var entry in newEntries)
                await _uow.CachedLocations.AddAsync(entry);
            await _uow.CompleteAsync(); // single transaction for the whole batch
        }

        return map;
    }

    // ============================================================
    //  PHASE 2 — DISTANCE  (now geocoding-free when given a map)
    // ============================================================

    /// <summary>
    /// Calculate driving distance (KILOMETERS) between pickup and dropoff with optional stops.
    /// If <paramref name="coordinateMap"/> is supplied (from ResolveAddressesAsync), this method
    /// does ZERO geocoding and ZERO DB access — it's a pure Directions API call.
    /// If the map is null it falls back to the old per-address geocode (backwards compatible).
    /// </summary>
    public async Task<Response<double>> CalculateDistanceAsync(
        LocationWithCoordinates pickup,
        LocationWithCoordinates dropoff,
        List<LocationWithCoordinates>? stops = null,
        IReadOnlyDictionary<string, (double Lat, double Lon)>? coordinateMap = null)
    {
        try
        {
            var pCoord = await ResolveAsync(pickup, coordinateMap);
            var dCoord = await ResolveAsync(dropoff, coordinateMap);

            if (pCoord is null || dCoord is null)
                return new Response<double>(HttpStatusCode.BadRequest, "Could not resolve locations");

            // Build waypoint path as (Lon, Lat) for Mapbox.
            var path = new List<(double Lon, double Lat)> { (pCoord.Value.Lon, pCoord.Value.Lat) };

            if (stops is { Count: > 0 })
            {
                foreach (var stop in stops)
                {
                    var sCoord = await ResolveAsync(stop, coordinateMap);
                    if (sCoord is { } s)
                        path.Add((s.Lon, s.Lat));
                    else
                        Console.WriteLine($"[Mapbox] ⚠️ Stop skipped (unresolved): {stop.Address}");
                }
            }

            path.Add((dCoord.Value.Lon, dCoord.Value.Lat));

            // Same pickup == dropoff and no real stops -> 0 km.
            if (path.Count == 2 &&
                AreCoordinatesClose(pCoord.Value.Lat, pCoord.Value.Lon, dCoord.Value.Lat, dCoord.Value.Lon))
            {
                return new Response<double>(HttpStatusCode.OK, 0.0);
            }

            var distanceMeters = await GetDirectionsDistanceAsync(path);
            return distanceMeters.HasValue
                ? new Response<double>(HttpStatusCode.OK, distanceMeters.Value / 1000.0)
                : new Response<double>(HttpStatusCode.OK, 0.0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mapbox] ❌ CalculateDistance error: {ex.Message}");
            return new Response<double>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// Resolve a single location: explicit coords -> pre-resolved map -> live geocode (fallback).
    /// </summary>
    private async Task<(double Lat, double Lon)?> ResolveAsync(
        LocationWithCoordinates loc,
        IReadOnlyDictionary<string, (double Lat, double Lon)>? coordinateMap)
    {
        if (loc.Latitude.HasValue && loc.Longitude.HasValue)
            return (loc.Latitude.Value, loc.Longitude.Value);

        var key = CleanAddress(loc.Address);

        if (coordinateMap is not null && coordinateMap.TryGetValue(key, out var mapped))
            return mapped;

        // Fallback path (single-trip callers that didn't pre-resolve).
        var geocoded = await GeocodeAddressAsync(key);
        return geocoded.StatusCode == (int)HttpStatusCode.OK ? geocoded.Data : null;
    }

    // ============================================================
    //  Single-address geocode (cache-aware) — kept for compatibility
    // ============================================================

    public async Task<Response<(double Lat, double Lon)>> GeocodeAddressAsync(string address)
    {
        try
        {
            var cached = await _uow.CachedLocations.GetByAddressAsync(address);
            if (cached != null)
                return new Response<(double, double)>(HttpStatusCode.OK, (cached.Latitude, cached.Longitude));

            var coord = await GeocodeViaHttpAsync(address);
            if (coord is not { } c)
                return new Response<(double, double)>(HttpStatusCode.NotFound, $"No coordinates found for: {address}");

            await _uow.CachedLocations.AddAsync(new CachedLocation
            {
                AddressName = address,
                Latitude = c.Lat,
                Longitude = c.Lon,
                CachedAt = DateTime.UtcNow
            });
            await _uow.CompleteAsync();

            return new Response<(double, double)>(HttpStatusCode.OK, c);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mapbox] ❌ Geocoding error: {ex.Message}");
            return new Response<(double, double)>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// PURE HTTP geocode. No cache, no DbContext — safe to call concurrently.
    /// Returns null on any failure so the caller decides what to do.
    /// </summary>
    private async Task<(double Lat, double Lon)?> GeocodeViaHttpAsync(string address)
    {
        var searchQuery = Uri.EscapeDataString($"{address}, Tashkent, Uzbekistan");
        var url = $"{GEOCODING_BASE_URL}/{searchQuery}.json?access_token={_accessToken}&limit=1";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[Mapbox] ❌ Geocode HTTP {(int)response.StatusCode} for '{address}'");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GeocodeResponseDto>(json, JsonOpts);

        var feature = result?.Features?.FirstOrDefault();
        if (feature?.Center is not { Count: >= 2 })
            return null;

        // Mapbox returns [lon, lat]
        return (feature.Center[1], feature.Center[0]);
    }

    // ============================================================
    //  Directions API
    // ============================================================

    private async Task<double?> GetDirectionsDistanceAsync(List<(double Lon, double Lat)> coordinates)
    {
        try
        {
            if (coordinates.Count < 2)
                return null;

            var coordString = string.Join(";", coordinates.Select(c => $"{c.Lon},{c.Lat}"));
            var url = $"{DIRECTIONS_BASE_URL}/{coordString}?access_token={_accessToken}&geometries=geojson&overview=full";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Mapbox] ❌ Directions API {(int)response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DirectionsResponseDto>(json, JsonOpts);

            var route = result?.Routes?.FirstOrDefault();
            return route?.Distance;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mapbox] ❌ Directions error: {ex.Message}");
            return null;
        }
    }

    // ============================================================
    //  Helpers
    // ============================================================

    private string CleanAddress(string rawAddress)
    {
        if (string.IsNullOrWhiteSpace(rawAddress))
            return string.Empty;

        var cleaned = Regex.Replace(rawAddress, @"^(PU|DO|ST):\s*", "", RegexOptions.IgnoreCase);
        return cleaned.Split(',')[0].Trim();
    }

    private bool AreCoordinatesClose(double lat1, double lon1, double lat2, double lon2)
        => CalculateHaversineDistance(lat1, lon1, lat2, lon2) <= COORDINATE_MATCH_THRESHOLD_METERS;

    private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // meters
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}