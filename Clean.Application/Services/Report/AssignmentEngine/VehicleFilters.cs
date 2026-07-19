using Clean.Application.Abstractions;
using Clean.Domain.Entities;

namespace Clean.Application.Services.Report.AssignmentEngine;

/// <summary>
/// Filters and prioritizes vehicles for trip assignments.
/// Handles vehicle availability, workload balancing, and driver limits per vehicle.
/// </summary>
public class VehicleFilters
{
    private readonly DriverFilters _driverFilters;
    private readonly IUnitOfWork _uow;

    private Dictionary<int, List<(DateTime Start, DateTime End)>>? _unavailablePeriodsCache;

    private const int MAX_DRIVERS_PER_VEHICLE = 4;

    public VehicleFilters(DriverFilters driverFilters, IUnitOfWork uow)
    {
        _driverFilters = driverFilters;
        _uow = uow;
    }

    /// <summary>
    /// Load unavailable periods cache for a date range (call once per period for efficiency)
    /// </summary>
    public async Task LoadUnavailablePeriodsForRangeAsync(DateTime startDate, DateTime endDate)
    {
        var allPeriods = await _uow.VehicleUnavailablePeriods.GetAllAsync();

        _unavailablePeriodsCache = allPeriods
            .Where(p => p.StartDate <= endDate && p.EndDate >= startDate)
            .GroupBy(p => p.VehicleId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => (p.StartDate, p.EndDate)).ToList()
            );
    }

    /// <summary>
    /// Check if vehicle is active on a specific date (considering ActiveFrom)
    /// </summary>
    public bool IsVehicleActiveOnDate(Domain.Entities.Vehicle vehicle, DateTime date)
    {
        if (!vehicle.ActiveFrom.HasValue)
            return true;

        return date.Date >= vehicle.ActiveFrom.Value.Date;
    }

    /// <summary>
    /// Check if vehicle is in an unavailable period on a specific date
    /// </summary>
    public bool IsVehicleInUnavailablePeriod(int vehicleId, DateTime date)
    {
        if (_unavailablePeriodsCache == null)
            return false;

        if (!_unavailablePeriodsCache.TryGetValue(vehicleId, out var periods))
            return false;

        var dateOnly = date.Date;
        return periods.Any(p => p.Start <= dateOnly && p.End >= dateOnly);
    }

    /// <summary>
    /// Check if vehicle is available on a specific date (both ActiveFrom and UnavailablePeriods)
    /// </summary>
    public bool IsVehicleAvailableOnDate(Domain.Entities.Vehicle vehicle, DateTime date)
    {
        // Check 1: ActiveFrom
        if (!IsVehicleActiveOnDate(vehicle, date))
            return false;

        // Check 2: Unavailable periods
        if (IsVehicleInUnavailablePeriod(vehicle.Id, date))
            return false;

        return true;
    }

    /// <summary>
    /// Check if vehicle was active during any part of a period (ActiveFrom check)
    /// </summary>
    public bool IsVehicleActiveInPeriod(Domain.Entities.Vehicle vehicle, DateTime periodStart, DateTime periodEnd)
    {
        if (!vehicle.ActiveFrom.HasValue)
            return true;

        return vehicle.ActiveFrom.Value.Date <= periodEnd.Date;
    }

    /// <summary>
    /// Check if vehicle has any available days in a period (not fully blocked)
    /// </summary>
    public bool IsVehicleAvailableInPeriod(Domain.Entities.Vehicle vehicle, DateTime periodStart, DateTime periodEnd)
    {
        // First check ActiveFrom
        if (!IsVehicleActiveInPeriod(vehicle, periodStart, periodEnd))
            return false;

        // Check if vehicle is unavailable for the ENTIRE period
        if (_unavailablePeriodsCache == null)
            return true;

        if (!_unavailablePeriodsCache.TryGetValue(vehicle.Id, out var periods))
            return true;

        // Calculate effective start date
        var effectiveStart = vehicle.ActiveFrom.HasValue && vehicle.ActiveFrom.Value.Date > periodStart.Date
            ? vehicle.ActiveFrom.Value.Date
            : periodStart.Date;

        // Check if the entire effective range is covered by unavailable periods
        foreach (var (start, end) in periods)
        {
            if (start <= effectiveStart && end >= periodEnd.Date)
            {
                return false; // Fully blocked
            }
        }

        return true;
    }

    /// <summary>
    /// Get available vehicles for a trip, filtered by:
    /// - Active status
    /// - ActiveFrom date
    /// - Unavailable periods
    /// - Matching vehicle type
    /// - No time conflicts
    /// - Workload balancing
    /// </summary>
    public List<Domain.Entities.Vehicle> GetAvailableVehicles(
        Domain.Entities.Trip trip,
        List<Domain.Entities.Vehicle> allVehicles,
        List<DriverAssignment> existingAssignments,
        Dictionary<int, int> vehicleWorkload)
    {
        var tripStart = trip.GetStartDateTime();
        var tripEnd = trip.GetEndDateTime();
        var tripDate = trip.PickUpDate;

        // Filter vehicles
        var available = allVehicles
            .Where(v => v.IsActive)
            .Where(v => IsVehicleAvailableOnDate(v, tripDate)) // Check both ActiveFrom and UnavailablePeriods
            .Where(v => MatchesRequiredType(v, trip))
            .Where(v => !HasTimeConflict(v, tripStart, tripEnd, existingAssignments))
            .ToList();

        // Sort by workload (least loaded first)
        return available
            .OrderBy(v => vehicleWorkload.GetValueOrDefault(v.Id, 0))
            .ThenBy(v => v.PlateNumber)
            .ToList();
    }

    /// <summary>
    /// Check if vehicle matches the required type for the trip
    /// </summary>
    public bool MatchesRequiredType(Domain.Entities.Vehicle vehicle, Domain.Entities.Trip trip)
    {
        // If trip doesn't specify a type, any vehicle works
        if (trip.VehicleTypeId == null)
            return true;

        return vehicle.VehicleTypeId == trip.VehicleTypeId;
    }

    /// <summary>
    /// Check if vehicle has a time conflict with any existing assignment
    /// </summary>
    public bool HasTimeConflict(
        Domain.Entities.Vehicle vehicle,
        DateTime tripStart,
        DateTime tripEnd,
        List<DriverAssignment> existingAssignments)
    {
        var vehicleAssignments = existingAssignments
            .Where(a => a.VehicleId == vehicle.Id && !a.HasConflict)
            .ToList();

        foreach (var assignment in vehicleAssignments)
        {
            var existingStart = assignment.Trip.GetStartDateTime();
            var existingEnd = assignment.Trip.GetEndDateTime();

            // Check for overlap
            if (tripStart < existingEnd && tripEnd > existingStart)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Initialize or get the driver set for a vehicle
    /// </summary>
    public HashSet<int> GetOrInitializeDriverSet(
        int vehicleId,
        Dictionary<int, HashSet<int>> vehicleDriverCount,
        List<DriverAssignment> existingAssignments)
    {
        if (!vehicleDriverCount.ContainsKey(vehicleId))
        {
            // Initialize with drivers already assigned to this vehicle
            var existingDrivers = existingAssignments
                .Where(a => a.VehicleId == vehicleId && a.DriverId.HasValue && !a.HasConflict)
                .Select(a => a.DriverId!.Value)
                .ToHashSet();

            vehicleDriverCount[vehicleId] = existingDrivers;
        }

        return vehicleDriverCount[vehicleId];
    }

    /// <summary>
    /// Check if adding another driver to this vehicle would exceed the limit
    /// </summary>
    public bool CanAddDriverToVehicle(
        int driverId,
        int vehicleId,
        Dictionary<int, HashSet<int>> vehicleDriverCount)
    {
        if (!vehicleDriverCount.TryGetValue(vehicleId, out var drivers))
            return true;

        // If driver already assigned to this vehicle, it's fine
        if (drivers.Contains(driverId))
            return true;

        // Check if we'd exceed the limit
        return drivers.Count < MAX_DRIVERS_PER_VEHICLE;
    }
}