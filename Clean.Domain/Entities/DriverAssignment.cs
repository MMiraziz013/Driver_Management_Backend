using Clean.Domain.Enums;

namespace Clean.Domain.Entities;

public class DriverAssignment
{
    public int Id { get; set; }

    public string? ConfNumber { get; set; } = string.Empty;
    
    public int? DriverId { get; set; }
    public Driver? Driver { get; set; }

    public int TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public int? VehicleId { get; set; }  // Changed from int to int?
    public Vehicle? Vehicle { get; set; }  // Changed to nullable

    public AssignmentType AssignmentType { get; set; }
    public bool HasConflict { get; set; }
    public string? Notes { get; set; }
}
