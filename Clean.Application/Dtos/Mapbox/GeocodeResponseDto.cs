namespace Clean.Application.Dtos.Mapbox;

public class GeocodeResponseDto
{
    public List<GeocodeFeatureDto> Features { get; set; } = new();
}