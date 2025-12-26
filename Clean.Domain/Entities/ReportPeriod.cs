using Clean.Domain.Enums;

namespace Clean.Domain.Entities;

public class ReportPeriod
{
    public int Id { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public string? Description { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedBy { get; set; }

    public ReportStatus Status { get; set; }

    public List<Trip> Trips { get; set; } = [];
}