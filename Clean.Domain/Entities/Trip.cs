namespace Clean.Domain.Entities;

public class Trip
{
    public int Id { get; set; }

    public DateTime PickUpDate { get; set; }
    public TimeSpan GarageOutTime { get; set; }
    public TimeSpan GarageInTime { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string RoutingDetails { get; set; } = string.Empty;

    public int VehicleTypeId { get; set; }
    public VehicleType VehicleType { get; set; } = null!;

    public int ServiceTypeId { get; set; }
    public ServiceType ServiceType { get; set; } = null!;

    public int ReportPeriodId { get; set; }
    public ReportPeriod ReportPeriod { get; set; } = null!;

    public bool IncludedInReport { get; set; } = true;

    public List<DriverAssignment> Assignments { get; set; } = new();
    
    // Correct way to combine Date and Time:
    public DateTime GetStartDateTime()
    {
        return PickUpDate.Date.Add(GarageOutTime);
    }

    public DateTime GetEndDateTime()
    {
        var start = GetStartDateTime();
        var end = PickUpDate.Date.Add(GarageInTime);

        // If In-Time is less than Out-Time, it means the trip finished the next morning
        if (GarageInTime < GarageOutTime)
        {
            end = end.AddDays(1);
        }

        return end;
    }
}