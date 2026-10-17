namespace Clean.Application.Dtos.VehicleType;

public class VehicleTypeFallbackDto
{
    public int VehicleTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class UpdateVehicleTypeFallbacksDto
{
    /// <summary>
    /// Fallback vehicle type ids in the order they should be tried
    /// </summary>
    public List<int> FallbackVehicleTypeIds { get; set; } = [];
}
