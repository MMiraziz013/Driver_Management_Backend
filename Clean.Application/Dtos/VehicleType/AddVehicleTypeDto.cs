using Clean.Domain.Enums;

namespace Clean.Application.Dtos.VehicleType;

public class AddVehicleTypeDto
{
    public string Name { get; set; } = string.Empty;  // Sedan, SUV, Minivan...

    public string? Description { get; set; }
    public int Capacity { get; set; }
}