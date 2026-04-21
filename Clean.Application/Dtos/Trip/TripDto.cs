namespace Clean.Application.Dtos.Trip;

public class TripDto
{
    public int Id { get; set; }
    public string ConfNumber { get; set; } = string.Empty;
    public DateTime PickUpDate { get; set; }
    public string GarageOutTime { get; set; } = string.Empty;
    public string GarageInTime { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string RoutingDetails { get; set; } = string.Empty;
    public double? DistanceKm { get; set; }
    public bool CoordinatesResolved { get; set; }
    public bool IncludedInReport { get; set; }
    public string? ImportedDriverName { get; set; }
    public string? ImportedVehiclePlate { get; set; }
    public string? PmtMethod { get; set; }
    public string VehicleTypeName { get; set; } = string.Empty;
    public string ServiceTypeName { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
    public int ServiceTypeId { get; set; }
    public int ReportPeriodId { get; set; }
}