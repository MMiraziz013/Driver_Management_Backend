namespace Clean.Domain.Entities;

public class FuelSettings
{
    public const double DefaultDurationFallbackSpeedKmh = 15.0;

    public int Id { get; set; }

    /// <summary>
    /// Assumed average speed (km per hour) used to estimate a trip's distance from its
    /// duration when the routing API returns nothing (and as a minimum for Custom Itinerary).
    /// </summary>
    public double DurationFallbackSpeedKmh { get; set; } = DefaultDurationFallbackSpeedKmh;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
