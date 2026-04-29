using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clean.Domain.Entities;

public class BonusSettings
{
    [Key]
    public int Id { get; set; }
    
    [MaxLength(100)]
    public string Name { get; set; } = "Default";
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // === Quantity-Based Rates (Transfer, To Airport, To Railway Station) ===
    [Column(TypeName = "decimal(18,2)")]
    public decimal QuantityPremiumVehicleRate { get; set; } = 100000;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal QuantityStandardVehicleRate { get; set; } = 75000;
    
    // === Quantity-Based "From Airport" Rates ===
    [Column(TypeName = "decimal(18,2)")]
    public decimal QuantityFromAirportPremiumRate { get; set; } = 125000;

    [Column(TypeName = "decimal(18,2)")]
    public decimal QuantityFromAirportStandardRate { get; set; } = 100000;

    // === Quantity-Based "From Railway" Rates ===
    [Column(TypeName = "decimal(18,2)")]
    public decimal QuantityFromRailwayPremiumRate { get; set; } = 115000;

    [Column(TypeName = "decimal(18,2)")]
    public decimal QuantityFromRailwayStandardRate { get; set; } = 90000;
    
    // === Round Trip Rates ===
    [Column(TypeName = "decimal(18,2)")]
    public decimal RoundTripPremiumVehicleRate { get; set; } = 125000;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal RoundTripStandardVehicleRate { get; set; } = 100000;
    
    // === Duration-Based Rates ===
    [Column(TypeName = "decimal(18,2)")]
    public decimal DurationUnder2HoursRate { get; set; } = 75000;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal DurationUnder4HoursRate { get; set; } = 150000;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Duration4To6HoursRate { get; set; } = 200000;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Duration6To8HoursRate { get; set; } = 250000;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Duration8To10HoursRate { get; set; } = 300000;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Duration10To12HoursRate { get; set; } = 350000;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Duration12To14HoursRate { get; set; } = 400000;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DurationOver14HoursRate { get; set; } = 450000;
    
    // === Field Trip Daily Rate (added on top of duration) ===
    [Column(TypeName = "decimal(18,2)")]
    public decimal FieldTripDailyRate { get; set; } = 100000;
    
    // === Premium Vehicle Types (JSON array) ===
    public string PremiumVehicleTypesJson { get; set; } = "[\"MB Sprinter\",\"VW Crafter\",\"Toyota Hiace\"]";
    
    [NotMapped]
    public List<string> PremiumVehicleTypes
    {
        get => string.IsNullOrEmpty(PremiumVehicleTypesJson) 
            ? new List<string>() 
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(PremiumVehicleTypesJson) ?? new List<string>();
        set => PremiumVehicleTypesJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}