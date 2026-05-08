using Clean.Domain.Enums;

namespace Clean.Domain.Entities;

public class Vehicle
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string Color { get; set; } = null!;

    public int VehicleTypeId { get; set; }
    public VehicleType VehicleType { get; set; } = null!;

    public DriverCategory RequiredDriverCategory { get; set; }

    public bool IsActive { get; set; } = true;

    // ===== FUEL MANAGEMENT PROPERTIES =====
    /// <summary>
    /// Fuel tank capacity in liters
    /// </summary>
    public double FuelTankCapacity { get; set; }

    /// <summary>
    /// Fuel consumption rate in liters per 100 kilometers
    /// Example: 11.0 means 11 liters per 100km
    /// </summary>
    public double FuelConsumptionPer100Km { get; set; }

    /// <summary>
    /// Type of fuel used by this vehicle: "АИ-92", "АИ-95", or "ДТ"
    /// </summary>
    public string FuelType { get; set; } = string.Empty;

    /// <summary>
    /// Initial fuel level at the start of the system (or period)
    /// Used as baseline for calculations
    /// </summary>
    public double InitialFuelLevel { get; set; }
    
    public double CurrentMileage { get; set; }
    
    public DateTime? MileageUpdatedAt { get; set; }

    public List<DriverAssignment> Assignments { get; set; } = [];
    public List<VehicleFuelAllocation> FuelAllocations { get; set; } = [];
    
    // ===== CAR REVENUE TRACKING =====

    /// <summary>
    /// Purchase cost of the vehicle in USD
    /// </summary>
    public decimal PurchaseCostUsd { get; set; }

    /// <summary>
    /// Number of months to divide purchase cost for plan calculation (default 13)
    /// </summary>
    public int PlanMonths { get; set; } = 13;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ===== HELPER METHODS =====
    
    /// <summary>
    /// Calculate fuel consumed for a given distance
    /// </summary>
    public double CalculateFuelConsumption(double distanceKm)
    {
        return distanceKm * (FuelConsumptionPer100Km / 100.0);
    }

    /// <summary>
    /// Check if a fuel type is compatible with this vehicle
    /// </summary>
    public bool IsCompatibleFuelType(string fuelType)
    {
        return FuelType.Equals(fuelType, StringComparison.OrdinalIgnoreCase);
    }
}
