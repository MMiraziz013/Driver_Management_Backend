using Clean.Domain.Entities;
using Clean.Domain.Enums;

namespace Clean.Application.Services.Report.AssignmentEngine;

/// <summary>
/// Contains all vehicle filter methods used during trip assignment.
/// </summary>
public class VehicleFilters
{
    private readonly DriverFilters _driverFilters;

    public VehicleFilters(DriverFilters driverFilters)
    {
        _driverFilters = driverFilters;
    }

    /// <summary>
    /// Check if vehicle is free during the given time range
    /// </summary>
    public bool IsVehicleFree(
        Domain.Entities.Vehicle vehicle,
        DateTime start,
        DateTime end,
        List<DriverAssignment> pendingAssignments)
    {
        var hasPendingConflict = pendingAssignments
            .Where(a => a.VehicleId == vehicle.Id && a.Trip != null)
            .Any(a => start < a.Trip.GetEndDateTime() && a.Trip.GetStartDateTime() < end);

        if (hasPendingConflict) return false;

        return !vehicle.Assignments.Any(a => a.Trip != null &&
            start < a.Trip.GetEndDateTime() && a.Trip.GetStartDateTime() < end);
    }

    /// <summary>
    /// Get available vehicles for a trip, filtered and sorted
    /// </summary>
    public List<Domain.Entities.Vehicle> GetAvailableVehicles(
        Domain.Entities.Trip trip,
        IEnumerable<Domain.Entities.Vehicle> allVehicles,
        List<DriverAssignment> pendingAssignments,
        Dictionary<int, int> vehicleWorkload)
    {
        var tripStart = trip.GetStartDateTime();
        var tripEnd = trip.GetEndDateTime();

        return allVehicles
            .Where(v => v.VehicleTypeId == trip.VehicleTypeId)
            .Where(v => IsVehicleFree(v, tripStart, tripEnd, pendingAssignments))
            .Where(v => !_driverFilters.IsBlockedByFieldTrip(null, v.Id, trip.PickUpDate.Date, pendingAssignments))
            .OrderBy(v => vehicleWorkload.GetValueOrDefault(v.Id, 0))
            .ThenBy(v => v.RequiredDriverCategory)
            .ToList();
    }

    /// <summary>
    /// Initialize or get the driver set for a vehicle
    /// </summary>
    public HashSet<int> GetOrInitializeDriverSet(
        int vehicleId,
        Dictionary<int, HashSet<int>> vehicleDriverCount,
        List<DriverAssignment> pendingAssignments)
    {
        if (!vehicleDriverCount.ContainsKey(vehicleId))
        {
            vehicleDriverCount[vehicleId] = new HashSet<int>(
                pendingAssignments
                    .Where(a => a.VehicleId == vehicleId && a.DriverId.HasValue)
                    .Select(a => a.DriverId!.Value)
            );
        }

        return vehicleDriverCount[vehicleId];
    }
}