using Clean.Domain.Enums;

namespace Clean.Domain.Entities;

public class VehicleType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;  // Sedan, SUV, Minivan...
    public string? Description { get; set; }
    
    public int Capacity { get; set; }

    // public DriverCategory RequiredCategory { get; set; }

    public List<Vehicle> Vehicles { get; set; } = [];
}
