namespace Clean.Application.Dtos.Trip;

public class TripDto
{
    public int Id { get; set; }
    public string ConfNumber { get; set; } = string.Empty;
    public DateTime PickUpDate { get; set; }
    public DateTime? OriginalPickUpDate { get; set; }
    public string GarageOutTime { get; set; } = string.Empty;
    public string GarageInTime { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string RoutingDetails { get; set; } = string.Empty;
    public double? DistanceKm { get; set; }
    public double? DrivingDurationHours { get; set; }
    public double IdleHours { get; set; }
    public bool CoordinatesResolved { get; set; }
    public bool IncludedInReport { get; set; }
    public string? ImportedDriverName { get; set; }
    public string? ImportedVehiclePlate { get; set; }
    public string? ImportedVehicleType { get; set; }
    public string? PmtMethod { get; set; }
    public string VehicleTypeName { get; set; } = string.Empty;
    public string ServiceTypeName { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
    public int ServiceTypeId { get; set; }
    public int ReportPeriodId { get; set; }

    /// <summary>
    /// Vehicle and driver from the trip's successful assignment; null when unassigned or in conflict
    /// </summary>
    public int? AssignedVehicleId { get; set; }
    public string? AssignedVehiclePlate { get; set; }
    public int? AssignedDriverId { get; set; }
    public string? AssignedDriverName { get; set; }
}