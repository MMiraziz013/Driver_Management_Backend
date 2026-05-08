namespace Clean.Application.Dtos.VehicleType;

public class UpdateVehicleTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Capacity { get; set; }
}