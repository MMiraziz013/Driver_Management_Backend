namespace Clean.Application.Dtos.Report;

public class BulkMileageUpdateRequest
{
    public List<VehicleMileageUpdate> Updates { get; set; } = new();
}