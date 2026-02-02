using System.Net;
using System.Text.RegularExpressions;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Clean.Application.Services.Report.AssignmentEngine;

/// <summary>
/// Main service for automatically assigning drivers to trips.
/// Orchestrates the assignment process using filter and prioritization components.
/// </summary>
public class DriverAssignmentService
{
    private readonly IUnitOfWork _uow;
    private readonly DriverFilters _driverFilters;
    private readonly VehicleFilters _vehicleFilters;
    private readonly DriverPrioritization _prioritization;
    private readonly DriverStateTracker _stateTracker;

    private const int MAX_DRIVERS_PER_VEHICLE = 4;

    public DriverAssignmentService(IUnitOfWork uow)
    {
        _uow = uow;
        _driverFilters = new DriverFilters();
        _vehicleFilters = new VehicleFilters(_driverFilters);
        _prioritization = new DriverPrioritization();
        _stateTracker = new DriverStateTracker();
    }

    /// <summary>
    /// Run the auto-assignment algorithm for a period.
    /// </summary>
    public async Task<Response<string>> RunAutoAssignmentAsync(int periodId, bool useCarryover = false)
    {
        try
        {
            var period = await _uow.ReportPeriods.GetWithTripsAsync(periodId);
            if (period == null)
                return new Response<string>(HttpStatusCode.NotFound, "Period not found.");

            // Check if period is finalized
            if (period.IsFinalized || period.IsAssignmentFinalized)
            {
                return new Response<string>(HttpStatusCode.BadRequest,
                    "Cannot run assignment on a finalized period. Revert finalization first.");
            }

            // Delete existing assignments
            await DeleteExistingAssignmentsAsync(period);

            var drivers = (await _uow.Drivers.GetActiveDriversWithDetailsAsync()).ToList();
            var vehicles = (await _uow.Vehicles.GetAllAsync()).ToList();
            var trips = period.Trips.OrderBy(t => t.PickUpDate).ThenBy(t => t.GarageOutTime).ToList();

            // Initialize tracking
            var vehicleWorkload = vehicles.ToDictionary(v => v.Id, v => 0);
            var driverWorkload = drivers.ToDictionary(d => d.Id, d => 0);
            var vehicleDriverCount = new Dictionary<int, HashSet<int>>();
            var newAssignments = new List<DriverAssignment>();

            // Optionally load carryover state
            Dictionary<int, DriverStateTracker.DriverAvailabilityState>? driverStates = null;
            if (useCarryover)
            {
                driverStates = _stateTracker.LoadDriverStatesForPeriod(period, drivers);
                LogCarryoverStates(driverStates, drivers);
            }

            // Separate Field Trips from regular trips
            var fieldTrips = trips
                .Where(t => t.ServiceType?.Name.Equals("Field Trip", StringComparison.OrdinalIgnoreCase) ?? false)
                .ToList();

            var regularTrips = trips
                .Where(t => !(t.ServiceType?.Name.Equals("Field Trip", StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            // PASS 1: Pre-assign Field Trips
            Console.WriteLine($"\n=== PASS 1: PRE-ASSIGNING {fieldTrips.Count} FIELD TRIPS ===");
            await AssignFieldTripsAsync(fieldTrips, drivers, vehicles, newAssignments, 
                vehicleWorkload, driverWorkload, vehicleDriverCount, driverStates);

            // PASS 2: Auto-assign regular trips
            Console.WriteLine($"\n=== PASS 2: AUTO-ASSIGNING {regularTrips.Count} REGULAR TRIPS ===");
            await AssignRegularTripsAsync(regularTrips, trips, drivers, vehicles, newAssignments,
                vehicleWorkload, driverWorkload, vehicleDriverCount, driverStates);

            // Save assignments
            await _uow.Context.DriverAssignments.AddRangeAsync(newAssignments);
            await _uow.CompleteAsync();

            // Generate statistics
            var statsMessage = GenerateStatistics(newAssignments, driverWorkload, vehicleWorkload, vehicleDriverCount);
            Console.WriteLine(statsMessage);

            return new Response<string>(HttpStatusCode.OK, statsMessage, "Success");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message, ex.InnerException?.Message ?? "", ex.StackTrace ?? "" });
        }
    }

    private async Task DeleteExistingAssignmentsAsync(Domain.Entities.ReportPeriod period)
    {
        var existingAssignments = await _uow.Context.DriverAssignments
            .Where(a => period.Trips.Select(t => t.Id).Contains(a.TripId))
            .ToListAsync();

        if (existingAssignments.Any())
        {
            _uow.Context.DriverAssignments.RemoveRange(existingAssignments);
            await _uow.CompleteAsync();
        }
    }

    private Task AssignFieldTripsAsync(
        List<Domain.Entities.Trip> fieldTrips,
        List<Domain.Entities.Driver> drivers,
        List<Domain.Entities.Vehicle> vehicles,
        List<DriverAssignment> newAssignments,
        Dictionary<int, int> vehicleWorkload,
        Dictionary<int, int> driverWorkload,
        Dictionary<int, HashSet<int>> vehicleDriverCount,
        Dictionary<int, DriverStateTracker.DriverAvailabilityState>? driverStates)
    {
        foreach (var trip in fieldTrips)
        {
            var plateNumber = ExtractPlateNumber(trip.ImportedVehiclePlate);
            var fDriver = drivers.FirstOrDefault(d => 
                d.FullName.Equals(trip.ImportedDriverName?.Trim(), StringComparison.OrdinalIgnoreCase));
            var fVehicle = vehicles.FirstOrDefault(v => 
                v.PlateNumber.Equals(plateNumber, StringComparison.OrdinalIgnoreCase));

            if (fDriver != null && fVehicle != null)
            {
                var assignment = new DriverAssignment
                {
                    TripId = trip.Id,
                    ConfNumber = trip.ConfNumber,
                    Trip = trip,
                    DriverId = fDriver.Id,
                    Driver = fDriver,
                    VehicleId = fVehicle.Id,
                    Vehicle = fVehicle,
                    AssignmentType = AssignmentType.Manual,
                    Notes = "Field Trip - Reserved"
                };
                newAssignments.Add(assignment);

                // Track stats
                driverWorkload[fDriver.Id]++;
                vehicleWorkload[fVehicle.Id]++;
                
                if (!vehicleDriverCount.ContainsKey(fVehicle.Id))
                    vehicleDriverCount[fVehicle.Id] = new HashSet<int>();
                vehicleDriverCount[fVehicle.Id].Add(fDriver.Id);

                // Update state if using carryover
                if (driverStates != null && driverStates.ContainsKey(fDriver.Id))
                {
                    _stateTracker.UpdateDriverStateAfterTrip(driverStates[fDriver.Id], trip);
                }

                Console.WriteLine($"   ✓ Reserved: {fDriver.FullName} on {fVehicle.PlateNumber}");
            }
            else
            {
                newAssignments.Add(new DriverAssignment
                {
                    TripId = trip.Id,
                    ConfNumber = trip.ConfNumber,
                    Trip = trip,
                    HasConflict = true,
                    AssignmentType = AssignmentType.Manual,
                    Notes = "Field Trip - Driver/Vehicle Not Found"
                });
            }
        }

        return Task.CompletedTask;
    }

    private Task AssignRegularTripsAsync(
        List<Domain.Entities.Trip> regularTrips,
        List<Domain.Entities.Trip> allTrips,
        List<Domain.Entities.Driver> drivers,
        List<Domain.Entities.Vehicle> vehicles,
        List<DriverAssignment> newAssignments,
        Dictionary<int, int> vehicleWorkload,
        Dictionary<int, int> driverWorkload,
        Dictionary<int, HashSet<int>> vehicleDriverCount,
        Dictionary<int, DriverStateTracker.DriverAvailabilityState>? driverStates)
    {
        foreach (var trip in regularTrips)
        {
            var tripStart = trip.GetStartDateTime();
            var tripEnd = trip.GetEndDateTime();

            Console.WriteLine($"\n--- Trip {trip.ConfNumber} ({tripStart:MM-dd HH:mm}) ---");

            Domain.Entities.Driver? selectedDriver = null;
            Domain.Entities.Vehicle? selectedVehicle = null;

            // Get available vehicles
            var availableVehicles = _vehicleFilters.GetAvailableVehicles(
                trip, vehicles, newAssignments, vehicleWorkload);

            Console.WriteLine($"Available vehicles: {availableVehicles.Count}");

            foreach (var vehicle in availableVehicles)
            {
                _vehicleFilters.GetOrInitializeDriverSet(vehicle.Id, vehicleDriverCount, newAssignments);

                // Get eligible drivers with all filters applied
                var eligibleDrivers = GetEligibleDrivers(
                    drivers, vehicle, trip, allTrips, newAssignments, 
                    vehicleDriverCount, driverStates);

                if (!eligibleDrivers.Any())
                {
                    LogFilterDiagnostics(drivers, vehicle, trip, allTrips, newAssignments, vehicleDriverCount);
                    continue;
                }

                // Prioritize and select the best driver
                selectedDriver = _prioritization
                    .PrioritizeDrivers(eligibleDrivers, vehicle.Id, tripStart.Date, 
                        vehicleDriverCount, driverWorkload, newAssignments)
                    .FirstOrDefault();

                if (selectedDriver != null)
                {
                    selectedVehicle = vehicle;

                    vehicleWorkload[vehicle.Id]++;
                    driverWorkload[selectedDriver.Id]++;
                    vehicleDriverCount[vehicle.Id].Add(selectedDriver.Id);

                    // Update state if using carryover
                    if (driverStates != null && driverStates.ContainsKey(selectedDriver.Id))
                    {
                        _stateTracker.UpdateDriverStateAfterTrip(driverStates[selectedDriver.Id], trip);
                    }

                    var driverType = _prioritization.IsBackupDriver(selectedDriver) ? "🆘 BACKUP" : "✓";
                    Console.WriteLine($"{driverType} Assigned: {selectedDriver.FullName} to {selectedVehicle.PlateNumber}");
                    break;
                }
            }

            // Create assignment
            var assignment = CreateAssignment(trip, selectedDriver, selectedVehicle, availableVehicles.Count);
            newAssignments.Add(assignment);
        }

        return Task.CompletedTask;
    }

    private List<Domain.Entities.Driver> GetEligibleDrivers(
        List<Domain.Entities.Driver> drivers,
        Domain.Entities.Vehicle vehicle,
        Domain.Entities.Trip trip,
        List<Domain.Entities.Trip> allTrips,
        List<DriverAssignment> newAssignments,
        Dictionary<int, HashSet<int>> vehicleDriverCount,
        Dictionary<int, DriverStateTracker.DriverAvailabilityState>? driverStates)
    {
        var tripStart = trip.GetStartDateTime();
        var tripEnd = trip.GetEndDateTime();

        IEnumerable<Domain.Entities.Driver> eligible = drivers;

        // Apply all filters
        eligible = eligible.Where(d => _driverFilters.HasRequiredCategory(d, vehicle));
        eligible = eligible.Where(d => !_driverFilters.IsOnLeave(d, trip.PickUpDate));
        eligible = eligible.Where(d => !_driverFilters.HasOverlap(d, tripStart, tripEnd, newAssignments));
        
        // Use carryover-aware filters if state is available
        if (driverStates != null)
        {
            eligible = eligible.Where(d => 
                _stateTracker.HasSufficientRestWithCarryover(d, tripStart, driverStates[d.Id], newAssignments));
            eligible = eligible.Where(d => 
                _stateTracker.WithinWeeklyLimitsWithCarryover(d, tripStart, tripEnd, driverStates[d.Id], newAssignments));
        }
        else
        {
            eligible = eligible.Where(d => 
                _driverFilters.Has10HourRestFromPreviousShift(d, tripStart, newAssignments));
            eligible = eligible.Where(d => 
                _driverFilters.WithinWeeklyLimits(d, tripStart, tripEnd, newAssignments));
        }

        eligible = eligible.Where(d => _driverFilters.CanFitInto20HourShift(d, tripStart, tripEnd, newAssignments));
        eligible = eligible.Where(d => !_driverFilters.IsBlockedByFieldTrip(d.Id, null, trip.PickUpDate, newAssignments));
        eligible = eligible.Where(d => !_driverFilters.WouldConflictWithFieldTrip(d, tripStart, tripEnd, allTrips, newAssignments));
        eligible = eligible.Where(d => _driverFilters.MeetsVehicleDriverLimit(d, vehicle.Id, vehicleDriverCount));

        return eligible.ToList();
    }

    private DriverAssignment CreateAssignment(Domain.Entities.Trip trip, Domain.Entities.Driver? driver, Domain.Entities.Vehicle? vehicle, int availableVehicleCount)
    {
        if (driver != null && vehicle != null)
        {
            return new DriverAssignment
            {
                TripId = trip.Id,
                ConfNumber = trip.ConfNumber,
                Trip = trip,
                DriverId = driver.Id,
                Driver = driver,
                VehicleId = vehicle.Id,
                Vehicle = vehicle,
                HasConflict = false,
                AssignmentType = AssignmentType.Auto
            };
        }

        var conflictReasons = new List<string>();
        if (availableVehicleCount == 0)
        {
            conflictReasons.Add("No vehicles available");
        }
        else
        {
            conflictReasons.Add("No qualified drivers available");
        }

        Console.WriteLine($"✗ Conflict: {string.Join("; ", conflictReasons)}");

        return new DriverAssignment
        {
            TripId = trip.Id,
            ConfNumber = trip.ConfNumber,
            Trip = trip,
            HasConflict = true,
            AssignmentType = AssignmentType.Auto,
            Notes = string.Join("; ", conflictReasons)
        };
    }

    private void LogCarryoverStates(
        Dictionary<int, DriverStateTracker.DriverAvailabilityState> driverStates,
        List<Domain.Entities.Driver> drivers)
    {
        Console.WriteLine($"\n=== DRIVER STATES LOADED FROM PREVIOUS PERIOD ===");
        foreach (var ds in driverStates.Values.Where(s => s.LastTripEndTime.HasValue).Take(5))
        {
            var driver = drivers.First(d => d.Id == ds.DriverId);
            Console.WriteLine($"  {driver.FullName}: LastTrip={ds.LastTripEndTime:MM-dd HH:mm}, " +
                            $"WeekHours={ds.CurrentWeekHoursWorked:F1}, ConsecDays={ds.ConsecutiveDaysWorked}");
        }
        Console.WriteLine($"=================================================\n");
    }

    private void LogFilterDiagnostics(
        List<Domain.Entities.Driver> drivers,
        Domain.Entities.Vehicle vehicle,
        Domain.Entities.Trip trip,
        List<Domain.Entities.Trip> allTrips,
        List<DriverAssignment> newAssignments,
        Dictionary<int, HashSet<int>> vehicleDriverCount)
    {
        var tripStart = trip.GetStartDateTime();
        var tripEnd = trip.GetEndDateTime();

        Console.WriteLine($"\n❌ NO DRIVER FOUND for vehicle {vehicle.PlateNumber} (Trip {trip.ConfNumber})");

        var currentPool = drivers.ToList();

        LogFilterResult("Category Match", currentPool,
            currentPool.Where(d => _driverFilters.HasRequiredCategory(d, vehicle)).ToList());

        LogFilterResult("Not On Leave", currentPool,
            currentPool.Where(d => !_driverFilters.IsOnLeave(d, trip.PickUpDate)).ToList());

        LogFilterResult("Time Overlap", currentPool,
            currentPool.Where(d => !_driverFilters.HasOverlap(d, tripStart, tripEnd, newAssignments)).ToList());

        LogFilterResult("10h Rest Rule", currentPool,
            currentPool.Where(d => _driverFilters.Has10HourRestFromPreviousShift(d, tripStart, newAssignments)).ToList());

        LogFilterResult("20h Max Shift", currentPool,
            currentPool.Where(d => _driverFilters.CanFitInto20HourShift(d, tripStart, tripEnd, newAssignments)).ToList());

        LogFilterResult("Weekly Limits", currentPool,
            currentPool.Where(d => _driverFilters.WithinWeeklyLimits(d, tripStart, tripEnd, newAssignments)).ToList());

        LogFilterResult("Field Trip Block", currentPool,
            currentPool.Where(d => !_driverFilters.IsBlockedByFieldTrip(d.Id, null, trip.PickUpDate, newAssignments)).ToList());

        LogFilterResult("Future FT Conflict", currentPool,
            currentPool.Where(d => !_driverFilters.WouldConflictWithFieldTrip(d, tripStart, tripEnd, allTrips, newAssignments)).ToList());

        LogFilterResult("Vehicle Driver Limit", currentPool,
            currentPool.Where(d => _driverFilters.MeetsVehicleDriverLimit(d, vehicle.Id, vehicleDriverCount)).ToList());
    }

    private void LogFilterResult(string filterName, List<Domain.Entities.Driver> before, List<Domain.Entities.Driver> after)
    {
        var removed = before.Except(after).ToList();
        if (removed.Any())
        {
            Console.WriteLine($"  - {filterName}: Removed {removed.Count} ({string.Join(", ", removed.Select(d => d.FullName))})");
        }
    }

    private string GenerateStatistics(
        List<DriverAssignment> assignments,
        Dictionary<int, int> driverWorkload,
        Dictionary<int, int> vehicleWorkload,
        Dictionary<int, HashSet<int>> vehicleDriverCount)
    {
        var totalTrips = assignments.Count;
        var conflictCount = assignments.Count(a => a.HasConflict);
        var fieldTripCount = assignments.Count(a => a.AssignmentType == AssignmentType.Manual);
        var autoAssignedCount = totalTrips - conflictCount - fieldTripCount;

        var activeDriverWorkloads = driverWorkload.Where(kv => kv.Value > 0).Select(kv => kv.Value).ToList();
        int dMin = activeDriverWorkloads.Any() ? activeDriverWorkloads.Min() : 0;
        int dMax = activeDriverWorkloads.Any() ? activeDriverWorkloads.Max() : 0;
        double dAvg = activeDriverWorkloads.Any() ? activeDriverWorkloads.Average() : 0;

        var activeVehicleWorkloads = vehicleWorkload.Where(kv => kv.Value > 0).Select(kv => kv.Value).ToList();
        int vMin = activeVehicleWorkloads.Any() ? activeVehicleWorkloads.Min() : 0;
        int vMax = activeVehicleWorkloads.Any() ? activeVehicleWorkloads.Max() : 0;
        double vAvg = activeVehicleWorkloads.Any() ? activeVehicleWorkloads.Average() : 0;

        var vehiclesOverLimit = vehicleDriverCount
            .Where(kv => kv.Value.Count > MAX_DRIVERS_PER_VEHICLE)
            .Count();

        return $@"
                === ASSIGNMENT COMPLETE ===
                Total: {totalTrips} | Auto: {autoAssignedCount} | Field Trips: {fieldTripCount} | Conflicts: {conflictCount}

                DRIVER BALANCE ({activeDriverWorkloads.Count} drivers active):
                • Range: {dMin}-{dMax} trips (spread: {dMax - dMin})
                • Average: {dAvg:F1} trips/driver

                VEHICLE BALANCE ({activeVehicleWorkloads.Count} vehicles active):
                • Range: {vMin}-{vMax} trips (spread: {vMax - vMin})
                • Average: {vAvg:F1} trips/vehicle

                MAX DRIVERS PER VEHICLE: {(vehiclesOverLimit > 0 ? $"⚠️ {vehiclesOverLimit} vehicle(s) exceed limit" : "✓ All compliant")}";
                    }

    /// <summary>
    /// Extract plate number from format: "car_type(plate_number)"
    /// </summary>
    private string ExtractPlateNumber(string? vehicleInfo)
    {
        if (string.IsNullOrWhiteSpace(vehicleInfo))
            return string.Empty;

        var match = Regex.Match(vehicleInfo, @"\(([^)]+)\)");
        return match.Success ? match.Groups[1].Value.Trim() : vehicleInfo.Trim();
    }
}