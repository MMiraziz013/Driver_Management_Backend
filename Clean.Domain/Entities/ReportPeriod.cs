using Clean.Domain.Enums;

namespace Clean.Domain.Entities;

public class ReportPeriod
{
    public int Id { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public string? Description { get; set; }
    
    public bool IsFuelFinalized { get; set; } = false;
    
    public DateTime? FuelFinalizedAt { get; set; }
    public DateTime GeneratedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    public string? GeneratedBy { get; set; }
    
    public bool IsFinalized { get; set; }
    
    public DateTime? FinalizedAt { get; set; }
    
    public bool IsAssignmentFinalized { get; set; }
    
    public DateTime? AssignmentFinalizedAt { get; set; }
    
    /// <summary>
    /// Whether mileage has been finalized for this period
    /// </summary>
    public bool IsMileageFinalized { get; set; }

    /// <summary>
    /// When mileage was finalized
    /// </summary>
    public DateTime? MileageFinalizedAt { get; set; }

    /// <summary>
    /// Who finalized the period (user ID or name)
    /// </summary>
    public string? FinalizedBy { get; set; }
    
    public ReportStatus Status { get; set; }

    public List<Trip> Trips { get; set; } = [];
    
    /// <summary>
    /// Navigation property - driver states recorded at the end of this period
    /// </summary>
    public List<DriverPeriodState> DriverPeriodStates { get; set; } = [];
}