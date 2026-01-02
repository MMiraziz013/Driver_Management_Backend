using Clean.Domain.Entities;
using Clean.Domain.Enums;

namespace Clean.Application.Dtos.ReportPeriod;

public class GetReportPeriodDto
{
    public int Id { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public string? Description { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedBy { get; set; }

    public ReportStatus Status { get; set; }
}