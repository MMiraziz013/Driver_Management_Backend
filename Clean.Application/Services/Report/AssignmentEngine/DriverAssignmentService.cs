using System.Net;
using System.Text.RegularExpressions;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Report;
using Clean.Application.Dtos.Responses;
using Clean.Application.Extensions;
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
        _vehicleFilters = new VehicleFilters(_driverFilters, uow);  // Pass IUnitOfWork
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

            // Load vehicle unavailable periods for this period
            await _vehicleFilters.LoadUnavailablePeriodsForRangeAsync(period.StartDate, period.EndDate);

            // Delete existing assignments
            await DeleteExistingAssignmentsAsync(period);

            var drivers = (await _uow.Drivers.GetActiveDriversWithDetailsAsync()).ToList();
            var allVehicles = (await _uow.Vehicles.GetAllAsync()).ToList();
            
            // Filter vehicles: must be active AND available in this period
            var vehicles = allVehicles
                .Where(v => v.IsActive)
                .Where(v => _vehicleFilters.IsVehicleAvailableInPeriod(v, period.StartDate, period.EndDate))
                .ToList();
            
            Console.WriteLine($"Vehicles available for period: {vehicles.Count} (out of {allVehicles.Count} total)");
            
            // Log any vehicles excluded due to ActiveFrom or UnavailablePeriods
            var excludedVehicles = allVehicles
                .Where(v => v.IsActive)
                .Where(v => !_vehicleFilters.IsVehicleAvailableInPeriod(v, period.StartDate, period.EndDate))
                .ToList();
            
            if (excludedVehicles.Any())
            {
                Console.WriteLine($"Vehicles excluded (ActiveFrom/Unavailable): {string.Join(", ", excludedVehicles.Select(v => v.PlateNumber))}");
            }

            var trips = period.Trips.OrderBy(t => t.PickUpDate).ThenBy(t => t.GarageOutTime).ToList();
            
            // excluding cash trips
            trips = trips.ExcludeCashTrips().ToList();
            
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
                .Where(t=> !t.IsSamarkandTrip) // removing samarkand trips not to assign trips to them.
                .ToList();

            var regularTrips = trips
                .Where(t => !(t.ServiceType?.Name.Equals("Field Trip", StringComparison.OrdinalIgnoreCase) ?? false))
                .Where(t=> !t.IsSamarkandTrip) // removing samarkand trips not to assign trips to them.
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

    /// <summary>
    /// Retry the conflicting trips of a period on their fallback vehicle types, keeping every
    /// successful assignment as it is. A trip that finds a vehicle and driver is moved to that type.
    /// </summary>
    public async Task<Response<ResolveConflictsResultDto>> ResolveConflictsAsync(int periodId)
    {
        try
        {
            var (ctx, error) = await LoadConflictContextAsync(periodId);
            if (ctx == null)
                return error!;

            var (_, assignments, drivers, vehicles, vehicleTypes, fallbacks, allTrips,
                vehicleWorkload, driverWorkload, vehicleDriverCount, conflicts) = ctx;

            var result = new ResolveConflictsResultDto { TotalConflicts = conflicts.Count };

            Console.WriteLine($"\n=== RESOLVING {conflicts.Count} CONFLICTS ===");

            foreach (var conflict in conflicts)
            {
                var trip = conflict.Trip;
                var originalTypeName = vehicleTypes.GetValueOrDefault(trip.VehicleTypeId)?.Name ?? "";

                var item = new ResolvedConflictItemDto
                {
                    TripId = trip.Id,
                    ConfNumber = trip.ConfNumber,
                    PickUpDate = trip.PickUpDate,
                    OriginalVehicleType = originalTypeName
                };
                result.Items.Add(item);

                // Field trip conflicts come from the imported driver/plate, not from the vehicle type
                if (trip.ServiceType?.Name.Equals("Field Trip", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    item.Status = "Skipped";
                    item.Reason = "Field trip - fix the imported driver/vehicle manually";
                    result.Skipped++;
                    continue;
                }

                var candidates = VehicleTypeFallbacks.GetCandidates(trip.VehicleTypeId, fallbacks);
                if (!candidates.Any())
                {
                    item.Status = "Unresolved";
                    item.Reason = $"No fallback vehicle types configured for {originalTypeName}";
                    result.Remaining++;
                    continue;
                }

                Console.WriteLine($"\n--- Trip {trip.ConfNumber} ({trip.GetStartDateTime():MM-dd HH:mm}), {originalTypeName} ---");

                foreach (var candidateTypeId in candidates)
                {
                    Console.WriteLine($"Trying {vehicleTypes.GetValueOrDefault(candidateTypeId)?.Name}");

                    var (driver, vehicle, _) = TryAssignTrip(
                        trip, candidateTypeId, allTrips, drivers, vehicles, assignments,
                        vehicleWorkload, driverWorkload, vehicleDriverCount, null);

                    if (driver == null || vehicle == null)
                        continue;

                    var newType = vehicleTypes[candidateTypeId];

                    // Only scalar keys are set: drivers are untracked and must not be attached
                    conflict.DriverId = driver.Id;
                    conflict.VehicleId = vehicle.Id;
                    conflict.HasConflict = false;
                    conflict.AssignmentType = AssignmentType.Auto;
                    conflict.Notes = $"Resolved: vehicle type {originalTypeName} → {newType.Name}";

                    trip.VehicleTypeId = newType.Id;
                    trip.VehicleType = newType;

                    item.Status = "Resolved";
                    item.NewVehicleType = newType.Name;
                    item.DriverName = driver.FullName;
                    item.VehiclePlate = vehicle.PlateNumber;
                    result.Resolved++;
                    break;
                }

                if (item.Status != "Resolved")
                {
                    item.Status = "Unresolved";
                    item.Reason = $"No vehicle or qualified driver available in fallback types " +
                                  $"({string.Join(", ", candidates.Select(id => vehicleTypes.GetValueOrDefault(id)?.Name))})";
                    result.Remaining++;
                }
            }

            await _uow.CompleteAsync();

            var message = $"Resolved {result.Resolved} of {result.TotalConflicts} conflicts " +
                          $"({result.Remaining} remaining, {result.Skipped} skipped).";
            Console.WriteLine(message);

            return new Response<ResolveConflictsResultDto>(HttpStatusCode.OK, message, result);
        }
        catch (Exception ex)
        {
            return new Response<ResolveConflictsResultDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message, ex.InnerException?.Message ?? "", ex.StackTrace ?? "" });
        }
    }

    /// <summary>
    /// Move conflicting trips to the closest day in the period where a vehicle of
    /// their own type - or else a fallback type - and a driver are free. Field Trips never move and
    /// successful assignments are kept as they are.
    /// </summary>
    public async Task<Response<ResolveConflictsResultDto>> RescheduleConflictsAsync(int periodId)
    {
        try
        {
            var (ctx, error) = await LoadConflictContextAsync(periodId);
            if (ctx == null)
                return error!;

            var (period, assignments, drivers, vehicles, vehicleTypes, fallbacks, allTrips,
                vehicleWorkload, driverWorkload, vehicleDriverCount, conflicts) = ctx;

            var result = new ResolveConflictsResultDto { TotalConflicts = conflicts.Count };

            Console.WriteLine($"\n=== RESCHEDULING {conflicts.Count} CONFLICTS ===");

            foreach (var conflict in conflicts)
            {
                var trip = conflict.Trip;
                var originalDate = trip.PickUpDate;
                var originalTypeName = vehicleTypes.GetValueOrDefault(trip.VehicleTypeId)?.Name ?? "";

                var item = new ResolvedConflictItemDto
                {
                    TripId = trip.Id,
                    ConfNumber = trip.ConfNumber,
                    PickUpDate = originalDate,
                    OriginalVehicleType = originalTypeName
                };
                result.Items.Add(item);

                if (trip.ServiceType?.Name.Equals("Field Trip", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    item.Status = "Skipped";
                    item.Reason = "Field trips keep their date";
                    result.Skipped++;
                    continue;
                }

                var dates = TripReschedule.GetCandidateDates(originalDate, period.StartDate, period.EndDate);
                var typeIds = new List<int> { trip.VehicleTypeId };
                typeIds.AddRange(VehicleTypeFallbacks.GetCandidates(trip.VehicleTypeId, fallbacks));

                Console.WriteLine($"\n--- Trip {trip.ConfNumber} ({trip.GetStartDateTime():MM-dd HH:mm}), {originalTypeName} ---");

                foreach (var date in dates)
                {
                    // Every driver and vehicle filter reads the trip's own date, so try the trip on the new day
                    trip.PickUpDate = DateTime.SpecifyKind(date, originalDate.Kind);

                    foreach (var typeId in typeIds)
                    {
                        var (driver, vehicle, _) = TryAssignTrip(
                            trip, typeId, allTrips, drivers, vehicles, assignments,
                            vehicleWorkload, driverWorkload, vehicleDriverCount, null);

                        if (driver == null || vehicle == null)
                            continue;

                        var newType = vehicleTypes[typeId];
                        var notes = $"Rescheduled: {originalDate:dd.MM} → {date:dd.MM}";
                        if (typeId != trip.VehicleTypeId)
                            notes += $"; vehicle type {originalTypeName} → {newType.Name}";

                        // Only scalar keys are set: drivers are untracked and must not be attached
                        conflict.DriverId = driver.Id;
                        conflict.VehicleId = vehicle.Id;
                        conflict.HasConflict = false;
                        conflict.AssignmentType = AssignmentType.Auto;
                        conflict.Notes = notes;

                        trip.OriginalPickUpDate ??= originalDate;
                        trip.VehicleTypeId = newType.Id;
                        trip.VehicleType = newType;

                        item.Status = "Resolved";
                        item.NewPickUpDate = trip.PickUpDate;
                        item.NewVehicleType = typeId != typeIds[0] ? newType.Name : null;
                        item.DriverName = driver.FullName;
                        item.VehiclePlate = vehicle.PlateNumber;
                        result.Resolved++;
                        break;
                    }

                    if (item.Status == "Resolved")
                        break;
                }

                if (item.Status != "Resolved")
                {
                    trip.PickUpDate = originalDate;
                    item.Status = "Unresolved";
                    item.Reason = "No driver/vehicle available on any day of the period";
                    result.Remaining++;
                }
            }

            await _uow.CompleteAsync();

            var message = $"Rescheduled {result.Resolved} of {result.TotalConflicts} conflicts " +
                          $"({result.Remaining} remaining, {result.Skipped} skipped).";
            if (result.Resolved > 0)
                message += " Re-run fuel allocation if it was already calculated for this period.";
            Console.WriteLine(message);

            return new Response<ResolveConflictsResultDto>(HttpStatusCode.OK, message, result);
        }
        catch (Exception ex)
        {
            return new Response<ResolveConflictsResultDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message, ex.InnerException?.Message ?? "", ex.StackTrace ?? "" });
        }
    }

    /// <summary>
    /// Everything Resolve/Reschedule Conflicts need: the period's assignments as the pending list,
    /// trackers rebuilt from the kept ones, and the conflicts to work on.
    /// </summary>
    private sealed record ConflictContext(
        Domain.Entities.ReportPeriod Period,
        List<DriverAssignment> Assignments,
        List<Domain.Entities.Driver> Drivers,
        List<Domain.Entities.Vehicle> Vehicles,
        Dictionary<int, Domain.Entities.VehicleType> VehicleTypes,
        List<VehicleTypeFallback> Fallbacks,
        List<Domain.Entities.Trip> AllTrips,
        Dictionary<int, int> VehicleWorkload,
        Dictionary<int, int> DriverWorkload,
        Dictionary<int, HashSet<int>> VehicleDriverCount,
        List<DriverAssignment> Conflicts);

    private async Task<(ConflictContext? Context, Response<ResolveConflictsResultDto>? Error)> LoadConflictContextAsync(int periodId)
    {
        var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
        if (period == null)
            return (null, new Response<ResolveConflictsResultDto>(HttpStatusCode.NotFound, "Period not found."));

        if (period.IsFinalized || period.IsAssignmentFinalized)
        {
            return (null, new Response<ResolveConflictsResultDto>(HttpStatusCode.BadRequest,
                "Cannot change assignments on a finalized period. Revert finalization first."));
        }

        var assignments = period.Trips.SelectMany(t => t.Assignments).ToList();
        if (!assignments.Any())
        {
            return (null, new Response<ResolveConflictsResultDto>(HttpStatusCode.BadRequest,
                "No assignments found for this period. Run assignment first."));
        }

        await _vehicleFilters.LoadUnavailablePeriodsForRangeAsync(period.StartDate, period.EndDate);

        // This period's assignments live in the pending list, so drivers must not carry them as well
        var drivers = await _uow.Drivers.GetActiveDriversWithDetailsExcludingPeriodAsync(periodId);
        var vehicles = (await _uow.Vehicles.GetAllAsync())
            .Where(v => v.IsActive)
            .Where(v => _vehicleFilters.IsVehicleAvailableInPeriod(v, period.StartDate, period.EndDate))
            .ToList();

        var vehicleTypes = await _uow.Context.VehicleTypes.ToDictionaryAsync(vt => vt.Id);
        var fallbacks = await _uow.Context.VehicleTypeFallbacks.AsNoTracking().ToListAsync();

        var allTrips = period.Trips.ExcludeCashTrips().ToList();

        // Rebuild the trackers from the assignments that are kept
        var vehicleWorkload = vehicles.ToDictionary(v => v.Id, v => 0);
        var driverWorkload = drivers.ToDictionary(d => d.Id, d => 0);
        var vehicleDriverCount = new Dictionary<int, HashSet<int>>();

        foreach (var a in assignments.Where(a => !a.HasConflict))
        {
            if (a.VehicleId.HasValue)
                vehicleWorkload[a.VehicleId.Value] = vehicleWorkload.GetValueOrDefault(a.VehicleId.Value) + 1;
            if (a.DriverId.HasValue)
                driverWorkload[a.DriverId.Value] = driverWorkload.GetValueOrDefault(a.DriverId.Value) + 1;
        }

        var conflicts = assignments
            .Where(a => a.HasConflict)
            .OrderBy(a => a.Trip.PickUpDate)
            .ThenBy(a => a.Trip.GarageOutTime)
            .ToList();

        return (new ConflictContext(period, assignments, drivers, vehicles, vehicleTypes, fallbacks, allTrips,
            vehicleWorkload, driverWorkload, vehicleDriverCount, conflicts), null);
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

            // Also check if the vehicle is available on this specific trip date
            if (fVehicle != null && !_vehicleFilters.IsVehicleAvailableOnDate(fVehicle, trip.PickUpDate))
            {
                Console.WriteLine($"   ⚠️ Vehicle {fVehicle.PlateNumber} unavailable on {trip.PickUpDate:yyyy-MM-dd}");
                fVehicle = null; // Treat as not found
            }

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
                if (driverWorkload.ContainsKey(fDriver.Id))
                    driverWorkload[fDriver.Id]++;
                if (vehicleWorkload.ContainsKey(fVehicle.Id))
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
                var reason = fDriver == null && fVehicle == null 
                    ? "Driver/Vehicle Not Found" 
                    : fDriver == null 
                        ? "Driver Not Found" 
                        : "Vehicle Not Found or Unavailable";
                        
                newAssignments.Add(new DriverAssignment
                {
                    TripId = trip.Id,
                    ConfNumber = trip.ConfNumber,
                    Trip = trip,
                    HasConflict = true,
                    AssignmentType = AssignmentType.Manual,
                    Notes = $"Field Trip - {reason}"
                });
                
                Console.WriteLine($"   ✗ Conflict: {reason}");
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
            Console.WriteLine($"\n--- Trip {trip.ConfNumber} ({trip.GetStartDateTime():MM-dd HH:mm}) ---");

            var (selectedDriver, selectedVehicle, availableVehicleCount) = TryAssignTrip(
                trip, trip.VehicleTypeId, allTrips, drivers, vehicles, newAssignments,
                vehicleWorkload, driverWorkload, vehicleDriverCount, driverStates);

            // Create assignment
            var assignment = CreateAssignment(trip, selectedDriver, selectedVehicle, availableVehicleCount);
            newAssignments.Add(assignment);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Find a vehicle of the given type and a driver for it, and record the pick in the workload trackers.
    /// Does not add an assignment - the caller decides how to persist the result.
    /// </summary>
    private (Domain.Entities.Driver? Driver, Domain.Entities.Vehicle? Vehicle, int AvailableVehicleCount) TryAssignTrip(
        Domain.Entities.Trip trip,
        int vehicleTypeId,
        List<Domain.Entities.Trip> allTrips,
        List<Domain.Entities.Driver> drivers,
        List<Domain.Entities.Vehicle> vehicles,
        List<DriverAssignment> newAssignments,
        Dictionary<int, int> vehicleWorkload,
        Dictionary<int, int> driverWorkload,
        Dictionary<int, HashSet<int>> vehicleDriverCount,
        Dictionary<int, DriverStateTracker.DriverAvailabilityState>? driverStates)
    {
        var tripStart = trip.GetStartDateTime();

        // Get available vehicles (already filtered by availability in GetAvailableVehicles)
        var availableVehicles = _vehicleFilters.GetAvailableVehicles(
            trip, vehicles, newAssignments, vehicleWorkload, vehicleTypeId);

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
            var selectedDriver = _prioritization
                .PrioritizeDrivers(eligibleDrivers, vehicle.Id, tripStart.Date, 
                    vehicleDriverCount, driverWorkload, newAssignments)
                .FirstOrDefault();

            if (selectedDriver == null)
                continue;

            vehicleWorkload[vehicle.Id] = vehicleWorkload.GetValueOrDefault(vehicle.Id) + 1;
            driverWorkload[selectedDriver.Id] = driverWorkload.GetValueOrDefault(selectedDriver.Id) + 1;
            vehicleDriverCount[vehicle.Id].Add(selectedDriver.Id);

            // Update state if using carryover
            if (driverStates != null && driverStates.ContainsKey(selectedDriver.Id))
            {
                _stateTracker.UpdateDriverStateAfterTrip(driverStates[selectedDriver.Id], trip);
            }

            var driverType = _prioritization.IsBackupDriver(selectedDriver) ? "🆘 BACKUP" : "✓";
            Console.WriteLine($"{driverType} Assigned: {selectedDriver.FullName} to {vehicle.PlateNumber}");

            return (selectedDriver, vehicle, availableVehicles.Count);
        }

        return (null, null, availableVehicles.Count);
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
        - Range: {dMin}-{dMax} trips (spread: {dMax - dMin})
        - Average: {dAvg:F1} trips/driver

        VEHICLE BALANCE ({activeVehicleWorkloads.Count} vehicles active):
        - Range: {vMin}-{vMax} trips (spread: {vMax - vMin})
        - Average: {vAvg:F1} trips/vehicle

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