using Clean.Domain.Entities;

namespace Clean.Application.Services.Report.AssignmentEngine;

/// <summary>
/// Contains driver prioritization and sorting logic for assignment.
/// </summary>
public class DriverPrioritization
{
    private static readonly string[] BackupDriverNames = { "Shukhrat Saibov", "Sardor Ataev" };

    /// <summary>
    /// Check if driver is a backup driver (lower priority)
    /// </summary>
    public bool IsBackupDriver(Domain.Entities.Driver driver)
    {
        return BackupDriverNames.Contains(driver.FullName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if driver is already working on the target date
    /// </summary>
    public bool IsWorkingToday(
        Domain.Entities.Driver driver, 
        DateTime targetDate,
        List<DriverAssignment> pendingAssignments)
    {
        var hasExisting = driver.Assignments.Any(a => 
            a.Trip != null && a.Trip.GetStartDateTime().Date == targetDate);
        
        var hasPending = pendingAssignments.Any(a =>
            a.DriverId == driver.Id && 
            a.Trip != null && 
            a.Trip.GetStartDateTime().Date == targetDate);
        
        return hasExisting || hasPending;
    }

    /// <summary>
    /// Get the number of hours the driver has worked in their current shift
    /// </summary>
    public double GetCurrentShiftHours(
        Domain.Entities.Driver driver, 
        DateTime targetDate,
        List<DriverAssignment> pendingAssignments)
    {
        var dayTrips = driver.Assignments
            .Where(a => a.Trip != null && a.Trip.GetStartDateTime().Date == targetDate)
            .Concat(pendingAssignments
                .Where(a => a.DriverId == driver.Id && 
                           a.Trip != null && 
                           a.Trip.GetStartDateTime().Date == targetDate))
            .OrderBy(a => a.Trip.GetStartDateTime())
            .ToList();

        if (!dayTrips.Any()) return 0;

        var firstTripStart = dayTrips.Min(a => a.Trip.GetStartDateTime());
        var lastTripEnd = dayTrips.Max(a => a.Trip.GetEndDateTime());

        return (lastTripEnd - firstTripStart).TotalHours;
    }

    /// <summary>
    /// Get the number of days since the driver last worked
    /// </summary>
    public int GetDaysSinceLastWork(
        Domain.Entities.Driver driver, 
        DateTime targetDate,
        List<DriverAssignment> pendingAssignments)
    {
        var allAssignments = driver.Assignments
            .Where(a => a.Trip != null && a.Trip.GetStartDateTime().Date < targetDate)
            .Concat(pendingAssignments
                .Where(a => a.DriverId == driver.Id && 
                           a.Trip != null && 
                           a.Trip.GetStartDateTime().Date < targetDate))
            .ToList();

        if (!allAssignments.Any()) return int.MaxValue; // Never worked - highest priority

        var lastWorkDate = allAssignments.Max(a => a.Trip.GetStartDateTime().Date);
        return (targetDate - lastWorkDate).Days;
    }

    /// <summary>
    /// Get the total number of trips assigned to a driver
    /// </summary>
    public int GetTotalAssignmentCount(
        Domain.Entities.Driver driver, 
        List<DriverAssignment> pendingAssignments)
    {
        return driver.Assignments.Count + pendingAssignments.Count(a => a.DriverId == driver.Id);
    }

    /// <summary>
    /// Sort drivers by priority for assignment.
    /// Returns an ordered list of drivers based on the priority criteria.
    /// </summary>
    public IOrderedEnumerable<Domain.Entities.Driver> PrioritizeDrivers(
        IEnumerable<Domain.Entities.Driver> eligibleDrivers,
        int vehicleId,
        DateTime tripDate,
        Dictionary<int, HashSet<int>> vehicleDriverCount,
        Dictionary<int, int> driverWorkload,
        List<DriverAssignment> pendingAssignments)
    {
        var vehicleDrivers = vehicleDriverCount.GetValueOrDefault(vehicleId, new HashSet<int>());

        return eligibleDrivers
            // PRIORITY 0: Regular drivers ALWAYS before backup drivers
            .OrderBy(d => IsBackupDriver(d))

            // PRIORITY 1: Continuity (Is the driver already assigned to this vehicle?)
            .ThenByDescending(d => vehicleDrivers.Contains(d.Id))

            // PRIORITY 2: Efficiency (Is the driver already working today?)
            .ThenByDescending(d => IsWorkingToday(d, tripDate, pendingAssignments))

            // PRIORITY 3: Fairness (Who has the least total work in this period?)
            .ThenBy(d => driverWorkload.GetValueOrDefault(d.Id, 0))

            // PRIORITY 4: Recovery (Who hasn't worked in the longest time?)
            .ThenBy(d => GetDaysSinceLastWork(d, tripDate, pendingAssignments))

            // PRIORITY 5: Capacity (Who has the most hours left in their shift?)
            .ThenBy(d => GetCurrentShiftHours(d, tripDate, pendingAssignments));
    }
}