namespace Clean.Application.Services.Report.AssignmentEngine;

/// <summary>
/// Days a conflicting trip may be moved to.
/// </summary>
public static class TripReschedule
{
    /// <summary>
    /// Every other day of the period, closest first (-1, +1, -2, +2, ...), earlier day first on a tie.
    /// The original day is never included.
    /// </summary>
    public static List<DateTime> GetCandidateDates(DateTime originalDate, DateTime periodStart, DateTime periodEnd)
    {
        var candidates = new List<DateTime>();
        var maxShiftDays = (periodEnd.Date - periodStart.Date).Days;

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
