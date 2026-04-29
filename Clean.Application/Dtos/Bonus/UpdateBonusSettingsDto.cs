namespace Clean.Application.Dtos.Bonus;

public class UpdateBonusSettingsDto
{
    public decimal? QuantityPremiumVehicleRate { get; set; }
    public decimal? QuantityStandardVehicleRate { get; set; }
    public decimal? QuantityFromAirportPremiumRate { get; set; }
    public decimal? QuantityFromAirportStandardRate { get; set; }
    public decimal? QuantityFromRailwayPremiumRate { get; set; }
    public decimal? QuantityFromRailwayStandardRate { get; set; }
    public decimal? RoundTripPremiumVehicleRate { get; set; }
    public decimal? RoundTripStandardVehicleRate { get; set; }
    public decimal? DurationUnder2HoursRate { get; set; }
    public decimal? DurationUnder4HoursRate { get; set; }
    public decimal? Duration4To6HoursRate { get; set; }
    public decimal? Duration6To8HoursRate { get; set; }
    public decimal? Duration8To10HoursRate { get; set; }
    public decimal? Duration10To12HoursRate { get; set; }
    public decimal? Duration12To14HoursRate { get; set; }
    public decimal? DurationOver14HoursRate { get; set; }
    public decimal? FieldTripDailyRate { get; set; }
    public List<string>? PremiumVehicleTypes { get; set; }
}