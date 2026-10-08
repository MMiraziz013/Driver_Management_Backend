namespace Clean.Application.Dtos.Fuel;

public class FuelSettingsDto
{
    public int Id { get; set; }

    /// <summary>
    /// Assumed average speed (km per hour) used to estimate distance from trip duration
    /// </summary>
    public double DurationFallbackSpeedKmh { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class UpdateFuelSettingsDto
{
    public double? DurationFallbackSpeedKmh { get; set; }
}
