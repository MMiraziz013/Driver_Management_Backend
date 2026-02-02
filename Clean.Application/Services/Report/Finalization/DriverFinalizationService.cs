using Clean.Application.Abstractions;
using Clean.Application.Dtos.Driver;
using Clean.Application.Dtos.ReportPeriod;
using Clean.Domain.Entities;

namespace Clean.Application.Services.Report.Finalization;

/// <summary>
/// Handles driver state finalization - calculating ending states
/// and updating drivers for the next period.
/// </summary>
public class DriverFinalizationService
{
    /// <summary>
    /// Preview driver finalization without saving changes
    /// </summary>
    public DriverFinalizationSummary PreviewDriverFinalization(
        Domain.Entities.ReportPeriod period,
        List<Domain.Entities.Driver> drivers)
    {
        var summary = new DriverFinalizationSummary
        {
            DriverStates = new List<DriverStateDto>()
        };

        foreach (var driver in drivers.Where(d => d.IsActive))
        {
            var driverState = CalculateDriverEndingState(driver, period);
            summary.DriverStates.Add(driverState);
        }

        summary.DriversUpdated = summary.DriverStates.Count;
        summary.DriversWithWarnings = summary.DriverStates.Count(d => d.Warnings.Any());
        summary.TotalTripsAssigned = summary.DriverStates.Sum(d => d.TripsThisPeriod);
        summary.TotalHoursWorked = summary.DriverStates.Sum(d => d.HoursWorkedThisPeriod);

        return summary;
    }

    /// <summary>
    /// Finalize driver assignments and update driver states
    /// </summary>
    public async Task<DriverFinalizationSummary> FinalizeDriverAssignmentsAsync(
        Domain.Entities.ReportPeriod period,
        List<Domain.Entities.Driver> drivers,
        IDriverPeriodStateRepository driverPeriodStateRepository)
    {
        var summary = new DriverFinalizationSummary
        {
            DriverStates = new List<DriverStateDto>()
        };

        foreach (var driver in drivers.Where(d => d.IsActive))
        {
            var driverState = CalculateDriverEndingState(driver, period);
            summary.DriverStates.Add(driverState);

            // Create historical record
            var periodState = new DriverPeriodState
            {
                DriverId = driver.Id,
                ReportPeriodId = period.Id,
                LastTripEndTime = driverState.LastTripEndTime,
                IncompleteWeekHoursWorked = driverState.IncompleteWeekHours,
                IncompleteWeekStartDate = GetWeekStartDate(period.EndDate),
                ConsecutiveDaysWorked = driverState.ConsecutiveDaysWorked,
                LastRestDay = driverState.LastRestDay,
                TotalPeriodHoursWorked = driverState.HoursWorkedThisPeriod,
                TotalPeriodTrips = driverState.TripsThisPeriod,
                CreatedAt = DateTime.UtcNow
            };

            await driverPeriodStateRepository.AddAsync(periodState);

            // UPDATE driver's current state for next period
            driver.LastTripEndTime = driverState.LastTripEndTime;
            driver.CurrentWeekHoursWorked = driverState.IncompleteWeekHours;
            driver.CurrentWeekStartDate = GetWeekStartDate(period.EndDate);
            driver.ConsecutiveDaysWorked = driverState.ConsecutiveDaysWorked;
            driver.LastRestDay = driverState.LastRestDay;
            driver.UpdatedAt = DateTime.UtcNow;

            Console.WriteLine($"  Driver {driver.FullName}: {driverState.TripsThisPeriod} trips, " +
                              $"{driverState.HoursWorkedThisPeriod:F1}h worked, " +
                              $"LastTrip: {driverState.LastTripEndTime?.ToString("MM-dd HH:mm") ?? "N/A"}");
        }

        summary.DriversUpdated = summary.DriverStates.Count;
        summary.DriversWithWarnings = summary.DriverStates.Count(d => d.Warnings.Any());
        summary.TotalTripsAssigned = summary.DriverStates.Sum(d => d.TripsThisPeriod);
        summary.TotalHoursWorked = summary.DriverStates.Sum(d => d.HoursWorkedThisPeriod);

        return summary;
    }

    /// <summary>
    /// Calculate a driver's ending state at the end of a period
    /// </summary>
    public DriverStateDto CalculateDriverEndingState(Domain.Entities.Driver driver, Domain.Entities.ReportPeriod period)
    {
        var driverTrips = period.Trips
            .Where(t => t.Assignments.Any(a => a.DriverId == driver.Id && !a.HasConflict))
            .OrderBy(t => t.PickUpDate)
            .ThenBy(t => t.GarageOutTime)
            .ToList();

        var state = new DriverStateDto
        {
            DriverId = driver.Id,
            DriverName = driver.FullName,
            TripsThisPeriod = driverTrips.Count,
            Warnings = new List<string>()
        };

        if (!driverTrips.Any())
        {
            state.LastTripEndTime = driver.LastTripEndTime;
            state.ConsecutiveDaysWorked = 0;
            state.LastRestDay = period.EndDate;
            state.HasSufficientRest = true;
            return state;
        }

        // Find last trip end time
        var lastTrip = driverTrips.Last();
        state.LastTripEndTime = lastTrip.GetEndDateTime();

        // Calculate total hours worked in period
        double totalHours = 0;
        foreach (var trip in driverTrips)
        {
            totalHours += EstimateTripDuration(trip);
        }

        state.HoursWorkedThisPeriod = totalHours;

        // Calculate hours in the incomplete week
        var weekStart = GetWeekStartDate(period.EndDate);
        var tripsInLastWeek = driverTrips
            .Where(t => t.PickUpDate.Date >= weekStart)
            .ToList();

        double lastWeekHours = tripsInLastWeek.Sum(t => EstimateTripDuration(t));
        state.IncompleteWeekHours = lastWeekHours;

        // Calculate consecutive days worked at end of period
        var workDays = driverTrips
            .Select(t => t.PickUpDate.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        int consecutiveDays = 0;
        DateTime checkDate = period.EndDate.Date;

        for (int i = 0; i <= 15; i++) // Safety limit
        {
            if (workDays.Contains(checkDate))
            {
                consecutiveDays++;
                checkDate = checkDate.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        state.ConsecutiveDaysWorked = consecutiveDays;

        // Find last rest day
        var allDaysInPeriod = Enumerable.Range(0, (period.EndDate - period.StartDate).Days + 1)
            .Select(i => period.StartDate.AddDays(i).Date)
            .ToList();

        var restDays = allDaysInPeriod.Except(workDays).OrderByDescending(d => d).ToList();
        state.LastRestDay = restDays.FirstOrDefault();

        // Calculate hours since last trip ended
        if (state.LastTripEndTime.HasValue)
        {
            state.HoursSinceLastTrip = (DateTime.UtcNow - state.LastTripEndTime.Value).TotalHours;
        }

        // Add warnings
        if (state.ConsecutiveDaysWorked >= 6)
        {
            state.Warnings.Add($"Worked {state.ConsecutiveDaysWorked} consecutive days - needs rest day");
        }

        if (state.IncompleteWeekHours > 50)
        {
            state.Warnings.Add($"Worked {state.IncompleteWeekHours:F1}h in current week - approaching limit");
        }

        state.HasSufficientRest = state.HoursSinceLastTrip >= 11;

        return state;
    }

    private double EstimateTripDuration(Domain.Entities.Trip trip)
    {
        var start = trip.GetStartDateTime();
        var end = trip.GetEndDateTime();
        var duration = (end - start).TotalHours;

        if (duration <= 0 || duration > 24)
        {
            if (trip.DistanceKm.HasValue && trip.DistanceKm.Value > 0)
                return (trip.DistanceKm.Value / 40.0) + 1;
            return 8;
        }

        return duration;
    }

    private DateTime GetWeekStartDate(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}