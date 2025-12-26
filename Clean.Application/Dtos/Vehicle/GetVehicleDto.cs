namespace Clean.Application.Dtos.Vehicle;

public class GetVehicleDto
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string Color { get; set; } = string.Empty;
    public string VehicleTypeName { get; set; } = string.Empty; // Just the name, not the whole object
    public string RequiredDriverCategory { get; set; } = string.Empty;
}