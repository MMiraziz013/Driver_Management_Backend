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

    public List<DriverAssignment> Assignments { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
