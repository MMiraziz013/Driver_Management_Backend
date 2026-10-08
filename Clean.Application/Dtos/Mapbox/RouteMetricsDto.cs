namespace Clean.Application.Dtos.Mapbox;

public class RouteMetricsDto
{
    public double DistanceKm { get; set; }

    /// <summary>
    /// Driving time in hours. Null when the distance is not a routed one
    /// </summary>
    public double? DurationHours { get; set; }
}
