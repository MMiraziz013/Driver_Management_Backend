namespace Clean.Application.Dtos.Bonus;

public class BonusSettingsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "Default";
    public bool IsActive { get; set; }
    
    // Quantity-Based Rates (Transfer, To Airport, To Railway)
    public decimal QuantityPremiumVehicleRate { get; set; }
    public decimal QuantityStandardVehicleRate { get; set; }
    
    // From Airport Rates
    public decimal QuantityFromAirportPremiumRate { get; set; }
    public decimal QuantityFromAirportStandardRate { get; set; }
    
    // From Railway Rates
    public decimal QuantityFromRailwayPremiumRate { get; set; }
    public decimal QuantityFromRailwayStandardRate { get; set; }
    
    // Round Trip Rates
    public decimal RoundTripPremiumVehicleRate { get; set; }
    public decimal RoundTripStandardVehicleRate { get; set; }
    
    // Duration-Based Rates
    public decimal DurationUnder2HoursRate { get; set; }
    public decimal DurationUnder4HoursRate { get; set; }
    public decimal Duration4To6HoursRate { get; set; }
    public decimal Duration6To8HoursRate { get; set; }
    public decimal Duration8To10HoursRate { get; set; }
    public decimal Duration10To12HoursRate { get; set; }
    public decimal Duration12To14HoursRate { get; set; }
    public decimal DurationOver14HoursRate { get; set; }
    
    // Field Trip
    public decimal FieldTripDailyRate { get; set; }
    
    // Premium Vehicles
    public List<string> PremiumVehicleTypes { get; set; } = new();
}

