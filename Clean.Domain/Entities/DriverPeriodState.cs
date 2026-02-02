namespace Clean.Domain.Entities;

/// <summary>
/// Stores the ending state of a driver at the end of a report period.
/// This state is used as the starting point for the next period's assignments.
/// </summary>
public class DriverPeriodState
{
    public int Id { get; set; }

    /// <summary>
    /// The driver this state belongs to
    /// </summary>
    public int DriverId { get; set; }
    public Driver Driver { get; set; } = null!;

    /// <summary>
    /// The period this state was recorded at the END of
    /// </summary>
    public int ReportPeriodId { get; set; }
    public ReportPeriod ReportPeriod { get; set; } = null!;

    /// <summary>
    /// When the driver's last trip ended in this period.
    /// Used to calculate rest hours before first trip of next period.
    /// </summary>
    public DateTime? LastTripEndTime { get; set; }

    /// <summary>
    /// The last trip's return location (if relevant for next assignment)
    /// </summary>
    public string? LastTripEndLocation { get; set; }

    /// <summary>
    /// Hours worked in the incomplete week at end of period.
    /// If period ends on Wednesday, this is hours from Monday-Wednesday.
    /// </summary>
    public double IncompleteWeekHoursWorked { get; set; }

    /// <summary>
    /// The start date of the incomplete week (for context)
    /// </summary>
    public DateTime? IncompleteWeekStartDate { get; set; }

    /// <summary>
    /// Number of consecutive days worked at end of period.
    /// Resets to 0 after a rest day.
    /// </summary>
    public int ConsecutiveDaysWorked { get; set; }

    /// <summary>
    /// Date of last rest day (full 24h off)
    /// </summary>
    public DateTime? LastRestDay { get; set; }

    /// <summary>
    /// Total hours worked in this period (for records)
    /// </summary>
    public double TotalPeriodHoursWorked { get; set; }

    /// <summary>
    /// Total trips assigned in this period
    /// </summary>
    public int TotalPeriodTrips { get; set; }

    /// <summary>
    /// When this state was recorded
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
