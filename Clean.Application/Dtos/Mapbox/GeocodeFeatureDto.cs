namespace Clean.Application.Dtos.Mapbox;

public class GeocodeFeatureDto
{
    public string PlaceName { get; set; } = string.Empty;
    public List<double> Center { get; set; } = new(); // [longitude, latitude]
}