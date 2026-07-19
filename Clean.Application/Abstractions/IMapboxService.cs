using Clean.Application.Dtos.Mapbox;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IMapboxService
{
    Task<Response<double>> CalculateDistanceAsync(
        LocationWithCoordinates pickup,
        LocationWithCoordinates dropoff,
        List<LocationWithCoordinates>? stops = null,
        IReadOnlyDictionary<string, (double Lat, double Lon)>? coordinateMap = null);

    Task<Response<(double Lat, double Lon)>> GeocodeAddressAsync(string address);

    Task<Dictionary<string, (double Lat, double Lon)>> ResolveAddressesAsync(IEnumerable<string> rawAddresses);
}