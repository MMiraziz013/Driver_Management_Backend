namespace Clean.Application.Dtos.Vehicle;

public class GetVehicleDto
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string Color { get; set; } = string.Empty;
    public string VehicleTypeName { get; set; } = string.Empty; // Just the name, not the whole object
    public string RequiredDriverCategory { get; set; } = string.Empty;
    
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
    
    public decimal PurchaseCostUsd { get; set; }
    public int PlanMonths { get; set; }
    
}