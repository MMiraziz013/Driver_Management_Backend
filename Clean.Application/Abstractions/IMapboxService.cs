using Clean.Application.Dtos.Mapbox;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IMapboxService
{
    /// <summary>
    /// Calculate distance between pickup and dropoff addresses (with optional stops)
    /// Returns distance in KILOMETERS
    /// </summary>
    Task<Response<double>> CalculateDistanceAsync(
        LocationWithCoordinates pickup,
        LocationWithCoordinates dropoff,
        List<LocationWithCoordinates>? stops = null);
    /// <summary>
    /// Geocode an address to latitude/longitude coordinates
    /// Returns (Latitude, Longitude)
    /// </summary>
    Task<Response<(double Lat, double Lon)>> GeocodeAddressAsync(string address);
}