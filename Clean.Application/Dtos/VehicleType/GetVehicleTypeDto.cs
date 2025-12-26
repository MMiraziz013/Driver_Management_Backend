namespace Clean.Application.Dtos.VehicleType;

public class GetVehicleTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;  // Sedan, SUV, Minivan...
    public string? Description { get; set; }
}