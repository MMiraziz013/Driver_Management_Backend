using Clean.Domain.Entities;

namespace Clean.Application.Services.Report.AssignmentEngine;

/// <summary>
/// Contains all driver filter methods used during trip assignment.
/// These methods determine if a driver is eligible for a specific trip.
/// </summary>
public class DriverFilters
{
    private const int MAX_DRIVERS_PER_VEHICLE = 4;
    private const int VEHICLE_SWITCH_BUFFER_MINUTES = 60;
    private const int MAX_CONSECUTIVE_DAYS = 6;
    private const int MIN_REST_HOURS = 10;

    /// <summary>
    /// Check if a driver is on vacation or has an off day
    /// </summary>
    public bool IsOnLeave(Domain.Entities.Driver driver, DateTime date)
    {
        var targetDate = DateOnly.FromDateTime(date);

        bool onVacation = driver.Vacations?.Any(v =>
            targetDate >= DateOnly.FromDateTime(v.StartDate) &&
            targetDate <= DateOnly.FromDateTime(v.EndDate)) ?? false;

        bool isOffDay = driver.OffDays?.Any(od =>
            DateOnly.FromDateTime(od.Date) == targetDate) ?? false;

        return onVacation || isOffDay;
    }

    /// <summary>
    /// Check if driver has overlapping trips at the given time
    /// </summary>
    public bool HasOverlap(
        Domain.Entities.Driver driver,
        DateTime start,
        DateTime end,
        List<DriverAssignment> pendingAssignments)
    {
        var hasPendingOverlap = pendingAssignments
            .Where(a => a.DriverId == driver.Id && a.Trip != null)
            .Any(a => start < a.Trip.GetEndDateTime() && a.Trip.GetStartDateTime() < end);

        if (hasPendingOverlap) return true;

        return driver.Assignments.Any(a => a.Trip != null &&
            start < a.Trip.GetEndDateTime() && a.Trip.GetStartDateTime() < end);
    }

    /// <summary>
    /// Check if driver has had 10-hour rest from previous shift
    /// Note: This always returns true because continuation is legal;
    /// the CanFitInto20HourShift check handles the actual limit.
    /// </summary>
    public bool Has10HourRestFromPreviousShift(
        Domain.Entities.Driver driver, 
        DateTime newTripStart, 
        List<DriverAssignment> currentAssignments)
    {
        var driverAssignments = currentAssignments
            .Where(a => a.DriverId == driver.Id && a.Trip != null)
            .OrderBy(a => a.Trip.GetStartDateTime())
            .ToList();

        if (!driverAssignments.Any()) return true;

        var lastTrip = driverAssignments
            .Where(a => a.Trip.GetEndDateTime() <= newTripStart)
            .OrderByDescending(a => a.Trip.GetEndDateTime())
            .FirstOrDefault();

        if (lastTrip == null) return true;

        var lastTripEnd = lastTrip.Trip.GetEndDateTime();
        var restHours = (newTripStart - lastTripEnd).TotalHours;

        // We return true because even if rest is < 10h, it's a "continuation."
        // The CanFitInto20HourShift will reject it if the total shift is too long.
        return true;
    }

    /// <summary>
    /// Check if adding this trip would create a shift longer than 20 hours
    /// </summary>
    public bool CanFitInto20HourShift(
        Domain.Entities.Driver driver, 
        DateTime newTripStart, 
        DateTime newTripEnd, 
        List<DriverAssignment> pendingAssignments)
    {
        var driverTrips = pendingAssignments
            .Where(a => a.DriverId == driver.Id && a.Trip != null)
            .Select(a => new { Start = a.Trip.GetStartDateTime(), End = a.Trip.GetEndDateTime() })
            .ToList();

        driverTrips.Add(new { Start = newTripStart, End = newTripEnd });

        var ordered = driverTrips.OrderBy(t => t.Start).ToList();

        foreach (var anchorTrip in ordered)
        {
            DateTime shiftStart = anchorTrip.Start;
            DateTime runningEnd = anchorTrip.End;

            foreach (var nextTrip in ordered.Where(t => t.Start > anchorTrip.Start))
            {
                double restGap = (nextTrip.Start - runningEnd).TotalHours;

                if (restGap >= 10)
                {
                    break;
                }

                runningEnd = nextTrip.End;
            }

            double totalSpan = (runningEnd - shiftStart).TotalHours;

            if (totalSpan > 20)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check if driver is within weekly hour and consecutive day limits
    /// </summary>
    public bool WithinWeeklyLimits(
        Domain.Entities.Driver driver, 
        DateTime tripStart, 
        DateTime tripEnd,
        List<DriverAssignment> pendingAssignments)
    {
        var allAssignments = driver.Assignments
            .Where(a => a.Trip != null)
            .Concat(pendingAssignments.Where(a => a.DriverId == driver.Id && a.Trip != null))
            .ToList();

        var workingDays = new HashSet<DateTime>();
        foreach (var a in allAssignments)
        {
            workingDays.Add(a.Trip.GetStartDateTime().Date);
        }

        var newTripDate = tripStart.Date;

        // Check 6-day limit: prevent 7 consecutive working days
        if (!workingDays.Contains(newTripDate))
        {
            var allDays = new HashSet<DateTime>(workingDays) { newTripDate };
            var sortedDays = allDays.OrderBy(d => d).ToList();

            int consecutiveDays = 1;
            for (int i = 1; i < sortedDays.Count; i++)
            {
                if ((sortedDays[i] - sortedDays[i - 1]).Days == 1)
                {
                    consecutiveDays++;
                    if (consecutiveDays > MAX_CONSECUTIVE_DAYS) return false;
                }
                else
                {
                    consecutiveDays = 1;
                }
            }
        }

        // Check 60-hour limit in any 7-day rolling window
        var earliestDate = allAssignments.Any()
            ? allAssignments.Min(a => a.Trip.GetStartDateTime().Date).AddDays(-6)
            : tripStart.Date.AddDays(-6);

        for (var windowStart = earliestDate; windowStart <= tripStart.Date; windowStart = windowStart.AddDays(1))
        {
            var windowEnd = windowStart.AddDays(7);

            var windowHours = allAssignments
                .Where(a => a.Trip.GetStartDateTime().Date >= windowStart && 
                           a.Trip.GetStartDateTime().Date < windowEnd)
                .Sum(a => (a.Trip.GetEndDateTime() - a.Trip.GetStartDateTime()).TotalHours);

            if (tripStart.Date >= windowStart && tripStart.Date < windowEnd)
            {
                windowHours += (tripEnd - tripStart).TotalHours;
            }

            if (windowHours > 60) return false;
        }

        return true;
    }

    /// <summary>
    /// Check if driver/vehicle is blocked by a Field Trip on the given date
    /// </summary>
    public bool IsBlockedByFieldTrip(
        int? driverId,
        int? vehicleId,
        DateTime tripDate,
        List<DriverAssignment> assignments)
    {
        var fieldTripDates = assignments
            .Where(a => a.AssignmentType == Domain.Enums.AssignmentType.Manual &&
                        a.Trip != null &&
                        ((driverId.HasValue && a.DriverId == driverId.Value) ||
                         (vehicleId.HasValue && a.VehicleId == vehicleId.Value)))
            .Select(a => a.Trip.PickUpDate.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (!fieldTripDates.Any()) return false;

        var checkDate = tripDate.Date;

        for (int i = 0; i < fieldTripDates.Count; i++)
        {
            var journeyStart = fieldTripDates[i];
            var journeyEnd = fieldTripDates[i];

            for (int j = i + 1; j < fieldTripDates.Count; j++)
            {
                if ((fieldTripDates[j] - journeyEnd).Days <= 1)
                {
                    journeyEnd = fieldTripDates[j];
                }
                else
                {
                    break;
                }
            }

            if (checkDate >= journeyStart && checkDate <= journeyEnd)
            {
                return true;
            }

            while (i < fieldTripDates.Count - 1 && fieldTripDates[i + 1] == journeyEnd)
            {
                i++;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if assigning this trip would conflict with a future Field Trip for this driver
    /// </summary>
    public bool WouldConflictWithFieldTrip(
        Domain.Entities.Driver driver,
        DateTime newTripStart,
        DateTime newTripEnd,
        List<Domain.Entities.Trip> allTrips,
        List<DriverAssignment> pendingAssignments)
    {
        var driverFieldTrips = allTrips
            .Where(t => t.ServiceType?.Name.Equals("Field Trip", StringComparison.OrdinalIgnoreCase) ?? false)
            .Where(t => !string.IsNullOrWhiteSpace(t.ImportedDriverName))
            .Where(t => t.ImportedDriverName!.Equals(driver.FullName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!driverFieldTrips.Any())
            return false;

        foreach (var fieldTrip in driverFieldTrips)
        {
            var fieldTripStart = fieldTrip.GetStartDateTime();
            var fieldTripEnd = fieldTrip.GetEndDateTime();

            // Check overlap
            if (newTripStart < fieldTripEnd && fieldTripStart < newTripEnd)
            {
                return true;
            }

            // Check insufficient time before Field Trip
            if (newTripEnd <= fieldTripStart)
            {
                var timeBetween = (fieldTripStart - newTripEnd).TotalMinutes;
                if (timeBetween < VEHICLE_SWITCH_BUFFER_MINUTES)
                {
                    return true;
                }
            }

            // Check insufficient time after Field Trip
            if (fieldTripEnd <= newTripStart)
            {
                var timeBetween = (newTripStart - fieldTripEnd).TotalMinutes;
                if (timeBetween < VEHICLE_SWITCH_BUFFER_MINUTES)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Check if driver needs vehicle switch time (1-hour buffer)
    /// </summary>
    public bool NeedsVehicleSwitchTime(
        Domain.Entities.Driver driver,
        int proposedVehicleId,
        DateTime newTripStart,
        List<DriverAssignment> pendingAssignments)
    {
        // CHECK 1: Does THIS DRIVER need time to switch from another vehicle?
        var allDriverAssignments = driver.Assignments
            .Where(a => a.Trip != null && a.VehicleId.HasValue)
            .Concat(pendingAssignments.Where(a =>
                a.DriverId == driver.Id &&
                a.Trip != null &&
                a.VehicleId.HasValue))
            .OrderByDescending(a => a.Trip.GetEndDateTime())
            .ToList();

        if (allDriverAssignments.Any())
        {
            var driverPreviousTrip = allDriverAssignments
                .FirstOrDefault(a => a.Trip.GetEndDateTime() <= newTripStart);

            if (driverPreviousTrip != null && driverPreviousTrip.VehicleId != proposedVehicleId)
            {
                var previousTripIsFieldTrip = driverPreviousTrip.Trip.ServiceType?.Name
                    .Equals("Field Trip", StringComparison.OrdinalIgnoreCase) ?? false;

                if (!previousTripIsFieldTrip)
                {
                    var timeBetweenTrips = (newTripStart - driverPreviousTrip.Trip.GetEndDateTime()).TotalMinutes;

                    if (timeBetweenTrips < VEHICLE_SWITCH_BUFFER_MINUTES)
                    {
                        return true;
                    }
                }
            }
        }

        // CHECK 2: Does THIS VEHICLE need time to switch from another driver?
        var allVehicleAssignments = pendingAssignments
            .Where(a => a.VehicleId == proposedVehicleId && a.Trip != null && a.DriverId.HasValue)
            .ToList();

        if (allVehicleAssignments.Any())
        {
            var vehiclePreviousTrip = allVehicleAssignments
                .Where(a => a.Trip.GetEndDateTime() <= newTripStart)
                .OrderByDescending(a => a.Trip.GetEndDateTime())
                .FirstOrDefault();

            if (vehiclePreviousTrip != null && vehiclePreviousTrip.DriverId != driver.Id)
            {
                var previousTripIsFieldTrip = vehiclePreviousTrip.Trip.ServiceType?.Name
                    .Equals("Field Trip", StringComparison.OrdinalIgnoreCase) ?? false;

                if (!previousTripIsFieldTrip)
                {
                    var timeBetweenTrips = (newTripStart - vehiclePreviousTrip.Trip.GetEndDateTime()).TotalMinutes;

                    if (timeBetweenTrips < VEHICLE_SWITCH_BUFFER_MINUTES)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Check if driver meets the vehicle's driver count limit
    /// </summary>
    public bool MeetsVehicleDriverLimit(
        Domain.Entities.Driver driver,
        int vehicleId,
        Dictionary<int, HashSet<int>> vehicleDriverCount)
    {
        if (!vehicleDriverCount.ContainsKey(vehicleId))
            return true;

        return vehicleDriverCount[vehicleId].Count < MAX_DRIVERS_PER_VEHICLE ||
               vehicleDriverCount[vehicleId].Contains(driver.Id);
    }

    /// <summary>
    /// Check if driver has the required category for the vehicle
    /// </summary>
    public bool HasRequiredCategory(Domain.Entities.Driver driver, Domain.Entities.Vehicle vehicle)
    {
        return (int)driver.Category >= (int)vehicle.RequiredDriverCategory;
    }
}