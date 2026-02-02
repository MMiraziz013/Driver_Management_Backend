namespace Clean.Application.Dtos.Mapbox;

public class LocationWithCoordinates
{
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}