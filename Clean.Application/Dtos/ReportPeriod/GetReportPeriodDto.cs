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
    
    public int TripCount { get; set; }
    public int AssignedCount { get; set; }
    public int ConflictCount { get; set; }
    
    /// <summary>
    /// Whether the entire period has been finalized (both fuel and drivers)
    /// </summary>
    public bool IsFinalized { get; set; }
    
    /// <summary>
    /// When the period was fully finalized
    /// </summary>
    public DateTime? FinalizedAt { get; set; }
    
    /// <summary>
    /// Whether fuel allocation specifically has been finalized
    /// </summary>
    public bool IsFuelFinalized { get; set; }
    
    /// <summary>
    /// When fuel allocation was finalized
    /// </summary>
    public DateTime? FuelFinalizedAt { get; set; }
    
    /// <summary>
    /// Whether driver assignments specifically have been finalized
    /// </summary>
    public bool IsAssignmentFinalized { get; set; }
    
    /// <summary>
    /// When driver assignments were finalized
    /// </summary>
    public DateTime? AssignmentFinalizedAt { get; set; }
    
    public bool IsMileageFinalized { get; set; }
    
    public DateTime? MileageFinalizedAt { get; set; }
    
    public string? FinalizedBy { get; set; }

}