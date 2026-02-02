namespace Clean.Application.Dtos.Mapbox;

public class ParsedRoutingDetailsDto
{
    public LocationWithCoordinates? PickUp { get; set; }
    public LocationWithCoordinates? DropOff { get; set; }
    public List<LocationWithCoordinates> Stops { get; set; } = new();
}