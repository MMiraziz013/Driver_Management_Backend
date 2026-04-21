namespace Clean.Application.Dtos.Trip;

public class UpdateTripDto
{
    public int Id { get; set; }
    public string? ConfNumber { get; set; }
    public DateTime? PickUpDate { get; set; }
    public string? GarageOutTime { get; set; }
    public string? GarageInTime { get; set; }
    public string? CompanyName { get; set; }
    public string? RoutingDetails { get; set; }
    public double? DistanceKm { get; set; }
    public bool? IncludedInReport { get; set; }
    public string? ImportedDriverName { get; set; }
    public string? ImportedVehiclePlate { get; set; }
    public string? PmtMethod { get; set; }
    public int? VehicleTypeId { get; set; }
    public int? ServiceTypeId { get; set; }
}