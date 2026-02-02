namespace Clean.Application.Dtos.Mapbox;

public class DirectionsResponseDto
{
    public List<RouteDto> Routes { get; set; } = new();
    public string Code { get; set; } = string.Empty;
}