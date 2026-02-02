using Clean.Domain.Entities;

namespace Clean.Application.Services.Report.AssignmentEngine;

/// <summary>
/// Tracks driver availability state during assignment, including carryover from previous periods.
/// </summary>
public class DriverStateTracker
{
    private const int MIN_REST_HOURS = 10;
    private const double MAX_WEEKLY_HOURS = 60.0;
    private const int MAX_CONSECUTIVE_DAYS = 6;

    /// <summary>
    /// In-memory state tracking during assignment
    /// </summary>
    public class DriverAvailabilityState
    {
        public int DriverId { get; set; }
        public DateTime? LastTripEndTime { get; set; }
        public double CurrentWeekHoursWorked { get; set; }
        public DateTime? CurrentWeekStartDate { get; set; }
        public int ConsecutiveDaysWorked { get; set; }
        public DateTime? LastRestDay { get; set; }
        public double TodayHoursWorked { get; set; }
        public DateTime? TodayDate { get; set; }
    }

    /// <summary>
    /// Load driver states from previous period or initialize fresh
    /// </summary>
    public Dictionary<int, DriverAvailabilityState> LoadDriverStatesForPeriod(
        Domain.Entities.ReportPeriod period,
        List<Domain.Entities.Driver> drivers)
    {
        var states = new Dictionary<int, DriverAvailabilityState>();
        var periodWeekStart = GetWeekStartDate(period.StartDate);

        foreach (var driver in drivers)
        {
            var state = new DriverAvailabilityState
            {
                DriverId = driver.Id,
                LastTripEndTime = driver.LastTripEndTime,
                CurrentWeekHoursWorked = driver.CurrentWeekHoursWorked,
                CurrentWeekStartDate = driver.CurrentWeekStartDate,
                ConsecutiveDaysWorked = driver.ConsecutiveDaysWorked,
                LastRestDay = driver.LastRestDay
            };

            // If the stored week is different from this period's first week, check if reset needed
            if (state.CurrentWeekStartDate.HasValue && state.CurrentWeekStartDate.Value < periodWeekStart)
            {
                var storedWeekEnd = state.CurrentWeekStartDate.Value.AddDays(6);
                if (period.StartDate > storedWeekEnd)
                {
                    // New week started - reset
                    state.CurrentWeekHoursWorked = 0;
                    state.CurrentWeekStartDate = periodWeekStart;
                }
            }

            // Check if enough days passed to reset consecutive days
            if (state.LastTripEndTime.HasValue)
            {
                var daysSinceLastTrip = (period.StartDate - state.LastTripEndTime.Value.Date).Days;
                if (daysSinceLastTrip >= 1)
                {
                    state.ConsecutiveDaysWorked = 0;
                }
            }

            states[driver.Id] = state;
        }

        return states;
    }

    /// <summary>
    /// Update driver state after assigning a trip
    /// </summary>
    public void UpdateDriverStateAfterTrip(DriverAvailabilityState state, Domain.Entities.Trip trip)
    {
        var tripStart = trip.GetStartDateTime();
        var tripEnd = trip.GetEndDateTime();
        var tripDuration = (tripEnd - tripStart).TotalHours;

        // Sanity check
        if (tripDuration <= 0 || tripDuration > 24)
        {
            tripDuration = EstimateTripDuration(trip);
        }

        // Update last trip end time
        state.LastTripEndTime = tripEnd;

        // Update weekly hours if same week
        var tripWeekStart = GetWeekStartDate(trip.PickUpDate);
        if (state.CurrentWeekStartDate == tripWeekStart)
        {
            state.CurrentWeekHoursWorked += tripDuration;
        }
        else
        {
            // New week
            state.CurrentWeekStartDate = tripWeekStart;
            state.CurrentWeekHoursWorked = tripDuration;
        }

        // Update consecutive days
        if (state.TodayDate != trip.PickUpDate.Date)
        {
            if (state.TodayDate.HasValue && (trip.PickUpDate.Date - state.TodayDate.Value).Days == 1)
            {
                state.ConsecutiveDaysWorked++;
            }
            else if (state.TodayDate.HasValue && (trip.PickUpDate.Date - state.TodayDate.Value).Days > 1)
            {
                state.ConsecutiveDaysWorked = 1;
            }
            else if (!state.TodayDate.HasValue)
            {
                state.ConsecutiveDaysWorked = 1;
            }

            state.TodayDate = trip.PickUpDate.Date;
        }

        // Update today's hours
        if (state.TodayDate == trip.PickUpDate.Date)
        {
            state.TodayHoursWorked += tripDuration;
        }
        else
        {
            state.TodayDate = trip.PickUpDate.Date;
            state.TodayHoursWorked = tripDuration;
        }
    }

    /// <summary>
    /// Check if driver has sufficient rest considering carryover from previous period
    /// </summary>
    public bool HasSufficientRestWithCarryover(
        Domain.Entities.Driver driver,
        DateTime tripStart,
        DriverAvailabilityState state,
        List<DriverAssignment> currentAssignments)
    {
        // First check: rest from PREVIOUS period (carryover state)
        if (state.LastTripEndTime.HasValue)
        {
            var hoursSincePreviousPeriod = (tripStart - state.LastTripEndTime.Value).TotalHours;
            if (hoursSincePreviousPeriod < MIN_REST_HOURS)
            {
                return false;
            }
        }

        // Then check: rest from trips assigned in THIS run
        var driverAssignments = currentAssignments
            .Where(a => a.DriverId == driver.Id && !a.HasConflict)
            .ToList();

        if (!driverAssignments.Any())
        {
            return true;
        }

        var lastAssignment = driverAssignments
            .OrderByDescending(a => a.Trip.GetEndDateTime())
            .First();

        var lastTripEnd = lastAssignment.Trip.GetEndDateTime();
        var hoursSinceLastTrip = (tripStart - lastTripEnd).TotalHours;

        return hoursSinceLastTrip >= MIN_REST_HOURS;
    }

    /// <summary>
    /// Check weekly limits considering carryover hours from previous period
    /// </summary>
    public bool WithinWeeklyLimitsWithCarryover(
        Domain.Entities.Driver driver,
        DateTime tripStart,
        DateTime tripEnd,
        DriverAvailabilityState state,
        List<DriverAssignment> currentAssignments)
    {
        var tripWeekStart = GetWeekStartDate(tripStart);
        var tripDuration = (tripEnd - tripStart).TotalHours;

        // Start with carryover hours if same week
        double weeklyHours = 0;
        if (state.CurrentWeekStartDate.HasValue && state.CurrentWeekStartDate.Value == tripWeekStart)
        {
            weeklyHours = state.CurrentWeekHoursWorked;
        }

        // Add hours from assignments in THIS run (same week)
        var weekAssignments = currentAssignments
            .Where(a => a.DriverId == driver.Id &&
                        !a.HasConflict &&
                        GetWeekStartDate(a.Trip.PickUpDate) == tripWeekStart)
            .ToList();

        foreach (var assignment in weekAssignments)
        {
            var start = assignment.Trip.GetStartDateTime();
            var end = assignment.Trip.GetEndDateTime();
            weeklyHours += (end - start).TotalHours;
        }

        return (weeklyHours + tripDuration) <= MAX_WEEKLY_HOURS;
    }

    /// <summary>
    /// Check consecutive days limit with carryover
    /// </summary>
    public bool WithinConsecutiveDaysLimitWithCarryover(
        Domain.Entities.Driver driver,
        DateTime tripDate,
        DriverAvailabilityState state,
        List<DriverAssignment> currentAssignments)
    {
        // Get days worked in current assignment run
        var daysWorkedThisRun = currentAssignments
            .Where(a => a.DriverId == driver.Id && !a.HasConflict)
            .Select(a => a.Trip.PickUpDate.Date)
            .Distinct()
            .ToHashSet();

        // If already working this day, no additional consecutive day
        if (daysWorkedThisRun.Contains(tripDate.Date))
        {
            return true;
        }

        // Count consecutive days leading up to this trip
        int consecutiveDays = state.ConsecutiveDaysWorked;

        // Add days from this run that are consecutive with the carryover
        if (state.LastTripEndTime.HasValue)
        {
            var lastDate = state.LastTripEndTime.Value.Date;
            var sortedDays = daysWorkedThisRun.OrderBy(d => d).ToList();

            foreach (var day in sortedDays)
            {
                if ((day - lastDate).Days == 1)
                {
                    consecutiveDays++;
                    lastDate = day;
                }
                else if ((day - lastDate).Days > 1)
                {
                    // Gap found - reset
                    consecutiveDays = 1;
                    lastDate = day;
                }
            }

            // Check if adding tripDate would exceed limit
            if (!daysWorkedThisRun.Contains(tripDate.Date))
            {
                if ((tripDate.Date - lastDate).Days == 1)
                {
                    consecutiveDays++;
                }
                else if ((tripDate.Date - lastDate).Days > 1)
                {
                    consecutiveDays = 1;
                }
            }
        }
        else
        {
            // No carryover - just count days in this run
            consecutiveDays = daysWorkedThisRun.Count + (daysWorkedThisRun.Contains(tripDate.Date) ? 0 : 1);
        }

        return consecutiveDays <= MAX_CONSECUTIVE_DAYS;
    }

    /// <summary>
    /// Estimate trip duration in hours
    /// </summary>
    public double EstimateTripDuration(Domain.Entities.Trip trip)
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

    /// <summary>
    /// Get the Monday of the week containing the given date
    /// </summary>
    public DateTime GetWeekStartDate(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}