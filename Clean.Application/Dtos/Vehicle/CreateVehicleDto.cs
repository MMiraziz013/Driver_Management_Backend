using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Vehicle;

public class CreateVehicleDto
{
    public string PlateNumber { get; set; } = null!;
    public string? Model { get; set; }
    public string Color { get; set; } = null!;
    public int VehicleTypeId { get; set; }
    public DriverCategory RequiredDriverCategory { get; set; }
}