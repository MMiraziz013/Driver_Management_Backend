namespace Clean.Application.Dtos.Report;

public class ResolveConflictsResultDto
{
    public int TotalConflicts { get; set; }
    public int Resolved { get; set; }
    public int Remaining { get; set; }
    public int Skipped { get; set; }
    public List<ResolvedConflictItemDto> Items { get; set; } = [];
}

public class ResolvedConflictItemDto
{
    public int TripId { get; set; }
    public string? ConfNumber { get; set; }
    public DateTime PickUpDate { get; set; }
    public string OriginalVehicleType { get; set; } = string.Empty;
    public string? NewVehicleType { get; set; }
    public string? DriverName { get; set; }
    public string? VehiclePlate { get; set; }

    /// <summary>
    /// Resolved, Unresolved or Skipped
    /// </summary>
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
