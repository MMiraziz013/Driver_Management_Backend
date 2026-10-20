namespace Clean.Application.Services.Report.AssignmentEngine;

/// <summary>
/// Days a conflicting trip may be moved to.
/// </summary>
public static class TripReschedule
{
    public const int MaxShiftDays = 3;

    /// <summary>
    /// Closest days first (-1, +1, -2, +2, ...), earlier day first on a tie, all inside the period.
    /// The original day is never included.
    /// </summary>
    public static List<DateTime> GetCandidateDates(DateTime originalDate, DateTime periodStart, DateTime periodEnd,
        int maxShiftDays = MaxShiftDays)
    {
        var candidates = new List<DateTime>();

        for (var shift = 1; shift <= maxShiftDays; shift++)
        {
            foreach (var date in new[] { originalDate.AddDays(-shift), originalDate.AddDays(shift) })
            {
                if (date.Date >= periodStart.Date && date.Date <= periodEnd.Date)
                    candidates.Add(date);
            }
        }

        return candidates;
    }
}
