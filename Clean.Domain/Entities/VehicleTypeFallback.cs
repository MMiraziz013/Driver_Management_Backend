namespace Clean.Domain.Entities;

/// <summary>
/// A vehicle type a trip can be moved to when no vehicle of its own type can take it.
/// Lower priority is tried first.
/// </summary>
public class VehicleTypeFallback
{
    public int Id { get; set; }

    public int VehicleTypeId { get; set; }
    public VehicleType VehicleType { get; set; } = null!;

    public int FallbackVehicleTypeId { get; set; }
    public VehicleType FallbackVehicleType { get; set; } = null!;

    public int Priority { get; set; }
}
