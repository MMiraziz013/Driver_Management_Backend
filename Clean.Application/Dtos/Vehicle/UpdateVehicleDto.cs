using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Vehicle;

public class UpdateVehicleDto
{
    public int Id { get; set; }
    public string? PlateNumber { get; set; } = null!;
    public string? Model { get; set; }
    public string? Color { get; set; } = null!;
    public int? VehicleTypeId { get; set; }
    public DriverCategory? RequiredDriverCategory { get; set; }
    
    public double FuelTankCapacity { get; set; }

    /// <summary>
    /// Fuel consumption rate in liters per 100 kilometers
    /// Example: 11.0 means 11 liters per 100km
    /// </summary>
    public double FuelConsumptionPer100Km { get; set; }

    /// <summary>
    /// Type of fuel used by this vehicle: "АИ-92", "АИ-95", or "ДТ"
    /// </summary>
    public string? FuelType { get; set; } = string.Empty;

    /// <summary>
    /// Initial fuel level at the start of the system (or period)
    /// Used as baseline for calculations
    /// </summary>
    public double InitialFuelLevel { get; set; }
    
    public double CurrentMileage { get; set; }
    
    public decimal PurchaseCostUsd { get; set; }
    public int? PlanMonths { get; set; }
    
    public DateTime? ActiveFrom { get; set; }
}