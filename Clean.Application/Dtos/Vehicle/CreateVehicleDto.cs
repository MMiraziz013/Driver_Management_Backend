using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Vehicle;

public class CreateVehicleDto
{
    public string PlateNumber { get; set; } = null!;
    public string? Model { get; set; }
    public string Color { get; set; } = null!;
    public int VehicleTypeId { get; set; }
    public DriverCategory RequiredDriverCategory { get; set; }
    
    public double FuelTankCapacity { get; set; }
    
    public double FuelConsumptionPer100Km { get; set; }
    
    /// <summary>
    /// Type of fuel used by this vehicle: "АИ-92", "АИ-95", or "ДТ"
    /// </summary>
    public string FuelType { get; set; } = string.Empty;
    
    public double InitialFuelLevel { get; set; }
    
    public double CurrentMileage { get; set; }
}