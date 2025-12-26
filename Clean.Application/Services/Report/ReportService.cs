using System.Globalization;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using MiniExcelLibs;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Clean.Application.Services.Report;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;

    public ReportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

public async Task<Response<string>> UploadReportAsync(IFormFile file, int periodId)
    {
        try
        {
            // 1. Delete all existing trips AND their assignments for this period
            var existingTrips = await _uow.Context.Trips
                .Where(t => t.ReportPeriodId == periodId)
                .ToListAsync();
            
            if (existingTrips.Any())
            {
                // Delete assignments first (foreign key constraint)
                var tripIds = existingTrips.Select(t => t.Id).ToList();
                var existingAssignments = await _uow.Context.DriverAssignments
                    .Where(a => tripIds.Contains(a.TripId))
                    .ToListAsync();
                
                if (existingAssignments.Any())
                {
                    _uow.Context.DriverAssignments.RemoveRange(existingAssignments);
                }
                
                // Then delete trips
                _uow.Context.Trips.RemoveRange(existingTrips);
                await _uow.CompleteAsync();
            }

            // 2. Now upload the new trips
            using var stream = file.OpenReadStream();
            var rows = await stream.QueryAsync(useHeaderRow: true);
            
            var vTypes = await _uow.VehicleTypes.GetAllAsync();
            var sTypes = await _uow.ServiceTypes.GetAllAsync();

            foreach (var row in rows)
            {
                string vName = row.VehicleType?.ToString() ?? "";
                string sName = row.ServiceType?.ToString() ?? "";

                var vType = vTypes.FirstOrDefault(x => x.Name.Equals(vName, StringComparison.OrdinalIgnoreCase));
                var sType = sTypes.FirstOrDefault(x => x.Name.Equals(sName, StringComparison.OrdinalIgnoreCase));

                if (vType == null || sType == null) continue;

                DateTime parsedDate = DateTime.ParseExact(row.PickUpDate.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var trip = new Domain.Entities.Trip
                {
                    ReportPeriodId = periodId,
                    PickUpDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc),
                    GarageOutTime = TimeSpan.Parse(row.GarageOutTime.ToString()),
                    GarageInTime = TimeSpan.Parse(row.GarageInTime.ToString()),
                    CompanyName = row.CompanyName?.ToString() ?? "Unknown",
                    RoutingDetails = row.RoutingDetails?.ToString() ?? "N/A",
                    VehicleTypeId = vType.Id,
                    ServiceTypeId = sType.Id,
                    IncludedInReport = true
                };

                await _uow.Trips.AddAsync(trip);
            }

            await _uow.CompleteAsync();
            return new Response<string>(HttpStatusCode.OK, "All trips replaced successfully.", "Success");
        }
        catch (Exception ex)
        {
            var errorMsg = ex.InnerException?.Message ?? ex.Message;
            return new Response<string>(HttpStatusCode.InternalServerError, new List<string> { errorMsg });
        }
    }

    public async Task<Response<string>> RunAutoAssignmentAsync(int periodId)
    {
        try
        {
            // 1. Load the period
            var period = await _uow.ReportPeriods.GetWithTripsAsync(periodId);
            if (period == null) return new Response<string>(HttpStatusCode.NotFound, "Period not found.");

            // 2. Delete existing assignments for this period
            var existingAssignments = await _uow.Context.DriverAssignments
                .Where(a => period.Trips.Select(t => t.Id).Contains(a.TripId))
                .ToListAsync();
            
            if (existingAssignments.Any())
            {
                _uow.Context.DriverAssignments.RemoveRange(existingAssignments);
                await _uow.CompleteAsync();
            }

            // 3. Fetch fresh data
            var drivers = await _uow.Drivers.GetActiveDriversWithDetailsAsync();
            var vehicles = await _uow.Vehicles.GetAllAsync();

            // 4. RUN ASSIGNMENT LOGIC
            var trips = period.Trips.OrderBy(t => t.PickUpDate).ThenBy(t => t.GarageOutTime).ToList();
            
            var newAssignments = new List<DriverAssignment>();
            
            // NEW: Track vehicle-driver pairings (max 2 drivers per vehicle)
            var vehicleDriverCount = new Dictionary<int, HashSet<int>>(); // vehicleId -> set of driverIds
            
            foreach (var trip in trips)
            {
                DateTime tripStart = trip.GetStartDateTime(); 
                DateTime tripEnd = trip.GetEndDateTime();

                Domain.Entities.Driver? selectedDriver = null;
                Domain.Entities.Vehicle? selectedVehicle = null;

                var availableVehicles = vehicles
                    .Where(v => v.VehicleTypeId == trip.VehicleTypeId && IsVehicleFree(v, tripStart, tripEnd, newAssignments))
                    .OrderBy(v => v.RequiredDriverCategory) 
                    .ToList();

                foreach (var v in availableVehicles)
                {
                    // Initialize tracking for this vehicle if not already tracked
                    if (!vehicleDriverCount.ContainsKey(v.Id))
                    {
                        vehicleDriverCount[v.Id] = new HashSet<int>(
                            newAssignments
                                .Where(a => a.VehicleId == v.Id && a.DriverId.HasValue)
                                .Select(a => a.DriverId.Value)
                        );
                    }
                    
                    selectedDriver = drivers
                        .Where(d => (int)d.Category >= (int)v.RequiredDriverCategory)
                        .Where(d => !IsOnLeave(d, trip.PickUpDate))
                        .Where(d => !HasOverlap(d, tripStart, tripEnd, newAssignments))
                        .Where(d => Has10HourRestFromPreviousShift(d, tripStart, newAssignments))
                        .Where(d => CanFitInto20HourShift(d, tripStart, tripEnd, newAssignments))
                        .Where(d => WithinWeeklyLimits(d, tripStart, tripEnd, newAssignments))
                        // NEW: Only allow if vehicle has <2 drivers OR this driver already drives it
                        .Where(d => vehicleDriverCount[v.Id].Count < 2 || vehicleDriverCount[v.Id].Contains(d.Id))
                        // PRIORITY 0: Strongly prefer drivers who already drive this vehicle
                        .OrderByDescending(d => vehicleDriverCount[v.Id].Contains(d.Id))
                        // PRIORITY 1: Drivers already working today (consolidate into shifts)
                        .ThenByDescending(d => IsWorkingToday(d, tripStart.Date, newAssignments))
                        // PRIORITY 2: Among those working, prefer those with most room in their shift
                        .ThenBy(d => GetCurrentShiftHours(d, tripStart.Date, newAssignments))
                        // PRIORITY 3: Drivers who haven't worked recently (distribute rest days)
                        .ThenBy(d => GetDaysSinceLastWork(d, tripStart.Date, newAssignments))
                        // PRIORITY 4: Balance total workload
                        .ThenBy(d => d.Assignments.Count + newAssignments.Count(a => a.DriverId == d.Id))
                        .FirstOrDefault();

                    if (selectedDriver != null)
                    {
                        selectedVehicle = v;
                        // Track this vehicle-driver pairing
                        vehicleDriverCount[v.Id].Add(selectedDriver.Id);
                        break; 
                    }
                }

                DriverAssignment assignment;
                
                if (selectedVehicle != null && selectedDriver != null)
                {
                    assignment = new DriverAssignment
                    {
                        TripId = trip.Id,
                        Trip = trip,
                        DriverId = selectedDriver.Id,
                        Driver = selectedDriver,
                        VehicleId = selectedVehicle.Id,
                        Vehicle = selectedVehicle,
                        HasConflict = false,
                        AssignmentType = AssignmentType.Auto
                    };
                }
                else
                {
                    assignment = new DriverAssignment 
                    { 
                        TripId = trip.Id,
                        Trip = trip,
                        HasConflict = true,
                        AssignmentType = AssignmentType.Auto,
                        Notes = "No available driver/vehicle found for this time slot."
                    };
                }
                
                newAssignments.Add(assignment);
            }

            // 5. Add all assignments at once
            await _uow.Context.DriverAssignments.AddRangeAsync(newAssignments);
            await _uow.CompleteAsync();
            
            return new Response<string>(HttpStatusCode.OK, "Auto-assignment completed.", "Success");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, new List<string> { ex.Message, ex.InnerException?.Message ?? "", ex.StackTrace ?? "" });
        }
    }

    public async Task<byte[]> ExportReportAsync(int periodId)
    {
        var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
        if (period == null) return [];

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Assignment Report");

        var headers = new[] { "Date", "Garage Out", "Garage In", "Service Type", "Route", "Driver", "Plate #", "Status", "Notes" };
        for (int i = 0; i < headers.Length; i++) {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int rowNum = 2;
        foreach (var trip in period.Trips.OrderBy(t => t.PickUpDate).ThenBy(t => t.GarageOutTime))
        {
            var assignment = trip.Assignments.FirstOrDefault();
            bool isConflict = assignment?.HasConflict ?? true;

            ws.Cell(rowNum, 1).Value = trip.PickUpDate.ToShortDateString();
            ws.Cell(rowNum, 2).Value = trip.GarageOutTime.ToString(@"hh\:mm");
            ws.Cell(rowNum, 3).Value = trip.GarageInTime.ToString(@"hh\:mm");
            ws.Cell(rowNum, 4).Value = trip.ServiceType?.Name ?? "N/A";
            ws.Cell(rowNum, 5).Value = trip.RoutingDetails;
            ws.Cell(rowNum, 6).Value = assignment?.Driver?.FullName ?? "UNASSIGNED";
            ws.Cell(rowNum, 7).Value = assignment?.Vehicle?.PlateNumber ?? "N/A";
            ws.Cell(rowNum, 8).Value = isConflict ? "CONFLICT" : "ASSIGNED";
            ws.Cell(rowNum, 9).Value = assignment?.Notes;

            if (isConflict)
            {
                ws.Range(rowNum, 1, rowNum, 9).Style.Fill.BackgroundColor = XLColor.IndianRed;
                ws.Range(rowNum, 1, rowNum, 9).Style.Font.FontColor = XLColor.White;
            }
            rowNum++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // --- LOGIC HELPERS ---

    private bool IsOnLeave(Domain.Entities.Driver d, DateTime date)
    {
        var targetDate = DateOnly.FromDateTime(date);

        bool onVacation = d.Vacations?.Any(v => 
            targetDate >= DateOnly.FromDateTime(v.StartDate) && 
            targetDate <= DateOnly.FromDateTime(v.EndDate)) ?? false;

        bool isOffDay = d.OffDays?.Any(od => 
            DateOnly.FromDateTime(od.Date) == targetDate) ?? false;

        return onVacation || isOffDay;
    }

    private bool HasOverlap(Domain.Entities.Driver d, DateTime start, DateTime end, List<DriverAssignment> pendingAssignments)
    {
        var hasPendingOverlap = pendingAssignments
            .Where(a => a.DriverId == d.Id && a.Trip != null)
            .Any(a => start < a.Trip.GetEndDateTime() && a.Trip.GetStartDateTime() < end);
        
        if (hasPendingOverlap) return true;
        
        return d.Assignments.Any(a => a.Trip != null && start < a.Trip.GetEndDateTime() && a.Trip.GetStartDateTime() < end);
    }

    private bool IsWorkingToday(Domain.Entities.Driver d, DateTime targetDate, List<DriverAssignment> pendingAssignments)
    {
        var hasExisting = d.Assignments.Any(a => a.Trip != null && a.Trip.GetStartDateTime().Date == targetDate);
        var hasPending = pendingAssignments.Any(a => a.DriverId == d.Id && a.Trip != null && a.Trip.GetStartDateTime().Date == targetDate);
        return hasExisting || hasPending;
    }

    private double GetCurrentShiftHours(Domain.Entities.Driver d, DateTime targetDate, List<DriverAssignment> pendingAssignments)
    {
        // Get all trips for this driver on target date
        var dayTrips = d.Assignments
            .Where(a => a.Trip != null && a.Trip.GetStartDateTime().Date == targetDate)
            .Concat(pendingAssignments
                .Where(a => a.DriverId == d.Id && a.Trip != null && a.Trip.GetStartDateTime().Date == targetDate))
            .OrderBy(a => a.Trip.GetStartDateTime())
            .ToList();

        if (!dayTrips.Any()) return 0;

        // Shift hours = from first trip start to last trip end
        var firstTripStart = dayTrips.Min(a => a.Trip.GetStartDateTime());
        var lastTripEnd = dayTrips.Max(a => a.Trip.GetEndDateTime());
        
        return (lastTripEnd - firstTripStart).TotalHours;
    }

    private int GetDaysSinceLastWork(Domain.Entities.Driver d, DateTime targetDate, List<DriverAssignment> pendingAssignments)
    {
        var allAssignments = d.Assignments
            .Where(a => a.Trip != null && a.Trip.GetStartDateTime().Date < targetDate)
            .Concat(pendingAssignments
                .Where(a => a.DriverId == d.Id && a.Trip != null && a.Trip.GetStartDateTime().Date < targetDate))
            .ToList();

        if (!allAssignments.Any()) return int.MaxValue; // Never worked - highest priority

        var lastWorkDate = allAssignments.Max(a => a.Trip.GetStartDateTime().Date);
        return (targetDate - lastWorkDate).Days;
    }

    private bool Has10HourRestFromPreviousShift(Domain.Entities.Driver d, DateTime newTripStart, List<DriverAssignment> pendingAssignments)
    {
        // Get all assignments (existing + pending)
        var allAssignments = d.Assignments
            .Where(a => a.Trip != null)
            .Concat(pendingAssignments.Where(a => a.DriverId == d.Id && a.Trip != null))
            .OrderBy(a => a.Trip.GetStartDateTime())
            .ToList();

        if (!allAssignments.Any()) return true; // No previous work

        // Find the last trip before the new trip start
        var previousTrips = allAssignments
            .Where(a => a.Trip.GetEndDateTime() <= newTripStart)
            .ToList();

        if (!previousTrips.Any()) return true; // No previous trips

        // Get the end time of the last previous trip
        var lastTripEnd = previousTrips.Max(a => a.Trip.GetEndDateTime());
        var lastTripDate = previousTrips.Where(a => a.Trip.GetEndDateTime() == lastTripEnd).First().Trip.GetStartDateTime().Date;

        // If same day, no rest check needed (can work continuously up to 20 hours)
        if (lastTripDate == newTripStart.Date) return true;

        // Different day - must have 10 hour rest
        var restHours = (newTripStart - lastTripEnd).TotalHours;
        return restHours >= 10;
    }

    private bool CanFitInto20HourShift(Domain.Entities.Driver d, DateTime newTripStart, DateTime newTripEnd, List<DriverAssignment> pendingAssignments)
    {
        // Get all trips on the same day as the new trip
        var targetDate = newTripStart.Date;
        
        var sameDayTrips = d.Assignments
            .Where(a => a.Trip != null && a.Trip.GetStartDateTime().Date == targetDate)
            .Concat(pendingAssignments
                .Where(a => a.DriverId == d.Id && a.Trip != null && a.Trip.GetStartDateTime().Date == targetDate))
            .ToList();

        if (!sameDayTrips.Any())
        {
            // First trip of the day - check if trip itself is under 20 hours
            return (newTripEnd - newTripStart).TotalHours <= 20;
        }

        // Calculate shift span: from earliest start to latest end (including new trip)
        var earliestStart = sameDayTrips.Min(a => a.Trip.GetStartDateTime());
        var latestEnd = sameDayTrips.Max(a => a.Trip.GetEndDateTime());

        // Include the new trip in calculations
        if (newTripStart < earliestStart) earliestStart = newTripStart;
        if (newTripEnd > latestEnd) latestEnd = newTripEnd;

        var totalShiftHours = (latestEnd - earliestStart).TotalHours;

        return totalShiftHours <= 20;
    }

    private bool WithinWeeklyLimits(Domain.Entities.Driver d, DateTime tripStart, DateTime tripEnd, List<DriverAssignment> pendingAssignments)
    {
        var allAssignments = d.Assignments
            .Where(a => a.Trip != null)
            .Concat(pendingAssignments.Where(a => a.DriverId == d.Id && a.Trip != null))
            .ToList();

        // Get all working days
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
                    if (consecutiveDays > 6) return false; // Would create 7 consecutive days
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
                .Where(a => a.Trip.GetStartDateTime().Date >= windowStart && a.Trip.GetStartDateTime().Date < windowEnd)
                .Sum(a => (a.Trip.GetEndDateTime() - a.Trip.GetStartDateTime()).TotalHours);
            
            if (tripStart.Date >= windowStart && tripStart.Date < windowEnd)
            {
                windowHours += (tripEnd - tripStart).TotalHours;
            }
            
            if (windowHours > 60) return false;
        }

        return true;
    }

    private bool IsVehicleFree(Domain.Entities.Vehicle v, DateTime start, DateTime end, List<DriverAssignment> pendingAssignments)
    {
        var hasPendingConflict = pendingAssignments
            .Where(a => a.VehicleId == v.Id && a.Trip != null)
            .Any(a => start < a.Trip.GetEndDateTime() && a.Trip.GetStartDateTime() < end);
        
        if (hasPendingConflict) return false;
        
        return !v.Assignments.Any(a => a.Trip != null && start < a.Trip.GetEndDateTime() && a.Trip.GetStartDateTime() < end);
    }
}