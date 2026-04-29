using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Bonus;
using Clean.Application.Dtos.Responses;
using Clean.Application.Extensions;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using ClosedXML.Excel;

namespace Clean.Application.Services.Bonus;

public class BonusCalculationService : IBonusCalculationService
{
    private readonly IUnitOfWork _uow;

    public BonusCalculationService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<BonusCalculationResultDto>> CalculateBonusesAsync(BonusCalculationRequestDto request)
    {
        try
        {
            if (request.PeriodIds == null || !request.PeriodIds.Any())
            {
                return new Response<BonusCalculationResultDto>(HttpStatusCode.BadRequest,
                    new List<string> { "At least one period must be selected" });
            }

            // Get settings
            var settings = await _uow.BonusSettings.GetActiveAsync();
            if (settings == null)
            {
                settings = new BonusSettings();
                await _uow.BonusSettings.AddAsync(settings);
                await _uow.CompleteAsync();
            }

            // Get service type configs
            var serviceTypeConfigs = await _uow.ServiceTypeBonusConfigs.GetAllWithServiceTypeAsync();
            var configByServiceTypeId = serviceTypeConfigs.ToDictionary(c => c.ServiceTypeId, c => c.CalculationMethod);

            // Get periods
            var periods = new List<Domain.Entities.ReportPeriod>();
            foreach (var periodId in request.PeriodIds)
            {
                var period = await _uow.ReportPeriods.GetByIdAsync(periodId);
                if (period != null)
                    periods.Add(period);
            }

            if (!periods.Any())
            {
                return new Response<BonusCalculationResultDto>(HttpStatusCode.NotFound,
                    new List<string> { "No valid periods found" });
            }

            // Get ALL trips for selected periods (INCLUDING cash trips!)
            var allTrips = new List<Domain.Entities.Trip>();
            foreach (var period in periods)
            {
                var periodTrips = await _uow.Trips.GetByPeriodWithDetailsAsync(period.Id);
                allTrips.AddRange(periodTrips);
            }

            // Filter only trips with driver names
            var tripsWithDrivers = allTrips
                .Where(t => !string.IsNullOrWhiteSpace(t.ImportedDriverName))
                .ToList();

            // Group by driver
            var tripsByDriver = tripsWithDrivers
                .GroupBy(t => t.ImportedDriverName!.Trim())
                .OrderBy(g => g.Key)
                .ToList();

            var driverResults = new List<DriverBonusResultDto>();

            foreach (var driverGroup in tripsByDriver)
            {
                var driverResult = CalculateDriverBonus(
                    driverGroup.Key,
                    driverGroup.ToList(),
                    settings,
                    configByServiceTypeId);

                driverResults.Add(driverResult);
            }

            var result = new BonusCalculationResultDto
            {
                PeriodIds = request.PeriodIds,
                PeriodNames = string.Join(", ", periods.Select(p => p.Description ?? $"{p.StartDate:dd.MM} - {p.EndDate:dd.MM.yyyy}")),
                CalculatedAt = DateTime.UtcNow,
                GrandTotal = driverResults.Sum(d => d.TotalBonus),
                TotalDrivers = driverResults.Count,
                TotalTrips = driverResults.Sum(d => d.TotalTrips),
                TotalHoursWorked = driverResults.Sum(d => d.TotalHoursWorked),
                DriverResults = driverResults.OrderByDescending(d => d.TotalBonus).ToList()
            };

            return new Response<BonusCalculationResultDto>(HttpStatusCode.OK, result);
        }
        catch (Exception ex)
        {
            return new Response<BonusCalculationResultDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message, ex.StackTrace ?? "" });
        }
    }

    private DriverBonusResultDto CalculateDriverBonus(
        string driverName,
        List<Domain.Entities.Trip> trips,
        BonusSettings settings,
        Dictionary<int, BonusCalculationMethod> configByServiceTypeId)
    {
        var premiumVehicleTypes = settings.PremiumVehicleTypes
            .Select(v => v.ToLowerInvariant())
            .ToHashSet();

        var serviceTypeBreakdowns = new List<ServiceTypeBonusBreakdownDto>();

        // Separate Sierra Nevada MB Sprinter Round Trips for special handling
        var sierraNevadaException = trips
            .Where(t => IsSierraNevadaSpinterRoundTrip(t))
            .ToList();

        var regularTrips = trips
            .Where(t => !IsSierraNevadaSpinterRoundTrip(t))
            .ToList();

        // Process Sierra Nevada exception trips as Duration-Based
        if (sierraNevadaException.Any())
        {
            var breakdown = CalculateServiceTypeBonus(
                "Round Trip (Sierra Nevada Exception)",
                sierraNevadaException,
                BonusCalculationMethod.DurationBased,  // Force duration-based
                settings,
                premiumVehicleTypes);

            serviceTypeBreakdowns.Add(breakdown);
        }

        // Group regular trips by service type
        var tripsByServiceType = regularTrips
            .GroupBy(t => new { t.ServiceTypeId, ServiceTypeName = t.ServiceType?.Name ?? "Unknown" })
            .ToList();

        foreach (var serviceTypeGroup in tripsByServiceType)
        {
            var serviceTypeId = serviceTypeGroup.Key.ServiceTypeId;
            var serviceTypeName = serviceTypeGroup.Key.ServiceTypeName;
            var serviceTrips = serviceTypeGroup.ToList();

            // Get calculation method for this service type
            var calcMethod = configByServiceTypeId.TryGetValue(serviceTypeId, out var method)
                ? method
                : BonusCalculationMethod.QuantityBased;

            var breakdown = CalculateServiceTypeBonus(
                serviceTypeName,
                serviceTrips,
                calcMethod,
                settings,
                premiumVehicleTypes);

            serviceTypeBreakdowns.Add(breakdown);
        }

        // Find longest and furthest trips (from ALL trips including exception)
        var longestTrip = trips
            .OrderByDescending(t => GetTripDurationHours(t))
            .FirstOrDefault();

        var furthestTrip = trips
            .Where(t => t.DistanceKm.HasValue)
            .OrderByDescending(t => t.DistanceKm)
            .FirstOrDefault();

        // Calculate unique days worked
        var uniqueDays = trips.Select(t => t.PickUpDate.Date).Distinct().Count();
        var totalHours = trips.Sum(t => GetTripDurationHours(t));

        return new DriverBonusResultDto
        {
            DriverName = driverName,
            TotalBonus = serviceTypeBreakdowns.Sum(b => b.BonusAmount),
            TotalHoursWorked = Math.Round(totalHours, 2),
            TotalTrips = trips.Count,
            TotalDaysWorked = uniqueDays,
            AverageHoursPerDay = uniqueDays > 0 ? Math.Round(totalHours / uniqueDays, 2) : 0,
            ServiceTypeBreakdowns = serviceTypeBreakdowns.OrderByDescending(b => b.BonusAmount).ToList(),
            LongestTrip = longestTrip != null ? MapToTripStat(longestTrip) : null,
            FurthestTrip = furthestTrip != null ? MapToTripStat(furthestTrip) : null
        };
    }

    private ServiceTypeBonusBreakdownDto CalculateServiceTypeBonus(
        string serviceTypeName,
        List<Domain.Entities.Trip> trips,
        BonusCalculationMethod method,
        BonusSettings settings,
        HashSet<string> premiumVehicleTypes)
    {
        var breakdown = new ServiceTypeBonusBreakdownDto
        {
            ServiceTypeName = serviceTypeName,
            CalculationMethod = method.ToString(),
            TripCount = trips.Count,
            TotalHours = Math.Round(trips.Sum(t => GetTripDurationHours(t)), 2)
        };

        decimal totalBonus = 0;

        switch (method)
        {
            case BonusCalculationMethod.QuantityBased:
                totalBonus = CalculateQuantityBasedBonus(trips, settings, premiumVehicleTypes, breakdown, serviceTypeName);
                break;

            case BonusCalculationMethod.RoundTripBased:
                totalBonus = CalculateRoundTripBonus(trips, settings, premiumVehicleTypes, breakdown);
                break;

            case BonusCalculationMethod.DurationBased:
                totalBonus = CalculateDurationBasedBonus(trips, settings, breakdown);
                break;

            case BonusCalculationMethod.FieldTripBased:
                totalBonus = CalculateFieldTripBonus(trips, settings, breakdown);
                break;
        }

        breakdown.BonusAmount = totalBonus;
        return breakdown;
    }

    private decimal CalculateQuantityBasedBonus(
        List<Domain.Entities.Trip> trips,
        BonusSettings settings,
        HashSet<string> premiumVehicleTypes,
        ServiceTypeBonusBreakdownDto breakdown,
        string serviceTypeName)
    {
        decimal total = 0;
    
        // Determine which rate category to use based on service type name
        bool isFromAirport = serviceTypeName.Equals("From Airport", StringComparison.OrdinalIgnoreCase);
        bool isFromRailway = serviceTypeName.Equals("From Railway Station", StringComparison.OrdinalIgnoreCase);

        foreach (var trip in trips)
        {
            var vehicleModel = TripQueryExtensions.ParseVehicleModel(trip.ImportedVehiclePlate);
            var isPremium = premiumVehicleTypes.Any(pv =>
                vehicleModel.Contains(pv, StringComparison.OrdinalIgnoreCase));

            decimal rate;
        
            if (isFromAirport)
            {
                rate = isPremium ? settings.QuantityFromAirportPremiumRate : settings.QuantityFromAirportStandardRate;
            }
            else if (isFromRailway)
            {
                rate = isPremium ? settings.QuantityFromRailwayPremiumRate : settings.QuantityFromRailwayStandardRate;
            }
            else
            {
                // Standard rates for Transfer, To Airport, To Railway Station
                rate = isPremium ? settings.QuantityPremiumVehicleRate : settings.QuantityStandardVehicleRate;
            }

            total += rate;
        
            if (isPremium)
                breakdown.PremiumVehicleTrips++;
            else
                breakdown.StandardVehicleTrips++;
        }

        return total;
    }

    private decimal CalculateRoundTripBonus(
        List<Domain.Entities.Trip> trips,
        BonusSettings settings,
        HashSet<string> premiumVehicleTypes,
        ServiceTypeBonusBreakdownDto breakdown)
    {
        decimal total = 0;

        foreach (var trip in trips)
        {
            var vehicleModel = TripQueryExtensions.ParseVehicleModel(trip.ImportedVehiclePlate);
            var isPremium = premiumVehicleTypes.Any(pv =>
                vehicleModel.ToLowerInvariant().Contains(pv));

            if (isPremium)
            {
                total += settings.RoundTripPremiumVehicleRate;
                breakdown.PremiumVehicleTrips++;
            }
            else
            {
                total += settings.RoundTripStandardVehicleRate;
                breakdown.StandardVehicleTrips++;
            }
        }

        return total;
    }

    private decimal CalculateDurationBasedBonus(
        List<Domain.Entities.Trip> trips,
        BonusSettings settings,
        ServiceTypeBonusBreakdownDto breakdown)
    {
        decimal total = 0;

        foreach (var trip in trips)
        {
            var duration = GetTripDurationHours(trip);

            if (duration < 2)
            {
                total += settings.DurationUnder2HoursRate;
                breakdown.TripsUnder2Hours++;
            }
            else if (duration < 4)
            {
                total += settings.DurationUnder4HoursRate;
                breakdown.TripsUnder4Hours++;
            }
            else if (duration < 6)
            {
                total += settings.Duration4To6HoursRate;
                breakdown.Trips4To6Hours++;
            }
            else if (duration < 8)
            {
                total += settings.Duration6To8HoursRate;
                breakdown.Trips6To8Hours++;
            }
            else if (duration < 10)
            {
                total += settings.Duration8To10HoursRate;
                breakdown.Trips8To10Hours++;
            }
            else if (duration < 12)
            {
                total += settings.Duration10To12HoursRate;
                breakdown.Trips10To12Hours++;
            }
            else if (duration < 14)
            {
                total += settings.Duration12To14HoursRate;
                breakdown.Trips12To14Hours++;
            }
            else
            {
                total += settings.DurationOver14HoursRate;
                breakdown.TripsOver14Hours++;
            }
        }

        return total;
    }

    private decimal CalculateFieldTripBonus(
        List<Domain.Entities.Trip> trips,
        BonusSettings settings,
        ServiceTypeBonusBreakdownDto breakdown)
    {
        // First, calculate duration-based bonus
        decimal durationBonus = CalculateDurationBasedBonus(trips, settings, breakdown);

        // Then, add daily rate for unique days
        var uniqueDays = trips.Select(t => t.PickUpDate.Date).Distinct().Count();
        decimal dailyBonus = uniqueDays * settings.FieldTripDailyRate;

        breakdown.TotalDays = uniqueDays;

        return durationBonus + dailyBonus;
    }

    /// <summary>
    /// Calculate trip duration in hours, handling overnight trips correctly.
    /// </summary>
    private double GetTripDurationHours(Domain.Entities.Trip trip)
    {
        var duration = trip.GarageInTime - trip.GarageOutTime;
        
        // Handle overnight trips (GarageInTime < GarageOutTime means it ended next day)
        if (duration.TotalHours < 0)
        {
            duration = duration.Add(TimeSpan.FromHours(24));
        }
        
        return duration.TotalHours;
    }

    private TripStatDto MapToTripStat(Domain.Entities.Trip trip) => new()
    {
        ConfNumber = trip.ConfNumber,
        Date = trip.PickUpDate,
        ServiceTypeName = trip.ServiceType?.Name ?? "Unknown",
        CompanyName = trip.CompanyName,
        VehicleInfo = trip.ImportedVehiclePlate ?? "Unknown",
        DurationHours = Math.Round(GetTripDurationHours(trip), 2),
        DistanceKm = trip.DistanceKm ?? 0
    };

    public async Task<Response<byte[]>> ExportBonusesToExcelAsync(BonusCalculationRequestDto request)
    {
        try
        {
            var calcResult = await CalculateBonusesAsync(request);
            if (calcResult.StatusCode != 200 || calcResult.Data == null)
            {
                return new Response<byte[]>(HttpStatusCode.BadRequest,
                    calcResult.Errors ?? new List<string> { "Calculation failed" });
            }

            var data = calcResult.Data;

            using var workbook = new XLWorkbook();

            // Summary sheet
            CreateSummarySheet(workbook, data);

            // Driver details sheet
            CreateDriverDetailsSheet(workbook, data);

            // Service type breakdown sheet
            CreateServiceTypeBreakdownSheet(workbook, data);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            
            return new Response<byte[]>(HttpStatusCode.OK, "Export successful", stream.ToArray());
        }
        catch (Exception ex)
        {
            return new Response<byte[]>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    private void CreateSummarySheet(XLWorkbook workbook, BonusCalculationResultDto data)
    {
        var ws = workbook.Worksheets.Add("Summary");

        ws.Cell(1, 1).Value = "DRIVER BONUS REPORT";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Range(1, 1, 1, 4).Merge();

        ws.Cell(2, 1).Value = $"Period: {data.PeriodNames}";
        ws.Cell(3, 1).Value = $"Generated: {data.CalculatedAt:dd.MM.yyyy HH:mm}";

        ws.Cell(5, 1).Value = "TOTALS";
        ws.Cell(5, 1).Style.Font.Bold = true;

        ws.Cell(6, 1).Value = "Total Drivers:";
        ws.Cell(6, 2).Value = data.TotalDrivers;

        ws.Cell(7, 1).Value = "Total Trips:";
        ws.Cell(7, 2).Value = data.TotalTrips;

        ws.Cell(8, 1).Value = "Total Hours Worked:";
        ws.Cell(8, 2).Value = Math.Round(data.TotalHoursWorked, 2);

        ws.Cell(9, 1).Value = "GRAND TOTAL BONUS:";
        ws.Cell(9, 1).Style.Font.Bold = true;
        ws.Cell(9, 2).Value = data.GrandTotal;
        ws.Cell(9, 2).Style.Font.Bold = true;
        ws.Cell(9, 2).Style.NumberFormat.Format = "#,##0";

        // Driver ranking table
        int row = 11;
        ws.Cell(row, 1).Value = "DRIVER RANKING";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Range(row, 1, row, 6).Merge();
        row++;

        var headers = new[] { "Rank", "Driver", "Trips", "Hours", "Days", "Bonus" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
            ws.Cell(row, i + 1).Style.Font.Bold = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        row++;

        int rank = 1;
        foreach (var driver in data.DriverResults.OrderByDescending(d => d.TotalBonus))
        {
            ws.Cell(row, 1).Value = rank++;
            ws.Cell(row, 2).Value = driver.DriverName;
            ws.Cell(row, 3).Value = driver.TotalTrips;
            ws.Cell(row, 4).Value = driver.TotalHoursWorked;
            ws.Cell(row, 5).Value = driver.TotalDaysWorked;
            ws.Cell(row, 6).Value = driver.TotalBonus;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private void CreateDriverDetailsSheet(XLWorkbook workbook, BonusCalculationResultDto data)
    {
        var ws = workbook.Worksheets.Add("Driver Details");

        var headers = new[]
        {
            "Driver", "Service Type", "Method", "Trips", "Hours",
            "Premium Vehicle", "Standard Vehicle",
            "<2h", "2-4h", "4-6h", "6-8h", "8-10h", "10-12h", "12-14h", ">14h", "Days",
            "Bonus"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var driver in data.DriverResults.OrderByDescending(d => d.TotalBonus))
        {
            foreach (var breakdown in driver.ServiceTypeBreakdowns)
            {
                int col = 1;
                ws.Cell(row, col++).Value = driver.DriverName;
                ws.Cell(row, col++).Value = breakdown.ServiceTypeName;
                ws.Cell(row, col++).Value = breakdown.CalculationMethod;
                ws.Cell(row, col++).Value = breakdown.TripCount;
                ws.Cell(row, col++).Value = breakdown.TotalHours;
                ws.Cell(row, col++).Value = breakdown.PremiumVehicleTrips;
                ws.Cell(row, col++).Value = breakdown.StandardVehicleTrips;
                ws.Cell(row, col++).Value = breakdown.TripsUnder2Hours;
                ws.Cell(row, col++).Value = breakdown.TripsUnder4Hours;
                ws.Cell(row, col++).Value = breakdown.Trips4To6Hours;
                ws.Cell(row, col++).Value = breakdown.Trips6To8Hours;
                ws.Cell(row, col++).Value = breakdown.Trips8To10Hours;
                ws.Cell(row, col++).Value = breakdown.Trips10To12Hours;
                ws.Cell(row, col++).Value = breakdown.Trips12To14Hours;
                ws.Cell(row, col++).Value = breakdown.TripsOver14Hours;
                ws.Cell(row, col++).Value = breakdown.TotalDays;
                ws.Cell(row, col++).Value = breakdown.BonusAmount;
                ws.Cell(row, col - 1).Style.NumberFormat.Format = "#,##0";
                row++;
            }

            // Driver total row
            ws.Cell(row, 1).Value = $"TOTAL: {driver.DriverName}";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Range(row, 1, row, 3).Merge();
            ws.Cell(row, 4).Value = driver.TotalTrips;
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Cell(row, 5).Value = driver.TotalHoursWorked;
            ws.Cell(row, 5).Style.Font.Bold = true;
            ws.Cell(row, 14).Value = driver.TotalBonus;
            ws.Cell(row, 14).Style.Font.Bold = true;
            ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0";
            ws.Range(row, 1, row, 14).Style.Fill.BackgroundColor = XLColor.LightYellow;
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private void CreateServiceTypeBreakdownSheet(XLWorkbook workbook, BonusCalculationResultDto data)
    {
        var ws = workbook.Worksheets.Add("By Service Type");

        // Aggregate by service type across all drivers
        var serviceTypeTotals = data.DriverResults
            .SelectMany(d => d.ServiceTypeBreakdowns)
            .GroupBy(b => b.ServiceTypeName)
            .Select(g => new
            {
                ServiceType = g.Key,
                TripCount = g.Sum(x => x.TripCount),
                TotalHours = g.Sum(x => x.TotalHours),
                TotalBonus = g.Sum(x => x.BonusAmount),
                Method = g.First().CalculationMethod
            })
            .OrderByDescending(x => x.TotalBonus)
            .ToList();

        var headers = new[] { "Service Type", "Method", "Total Trips", "Total Hours", "Total Bonus" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var st in serviceTypeTotals)
        {
            ws.Cell(row, 1).Value = st.ServiceType;
            ws.Cell(row, 2).Value = st.Method;
            ws.Cell(row, 3).Value = st.TripCount;
            ws.Cell(row, 4).Value = Math.Round(st.TotalHours, 2);
            ws.Cell(row, 5).Value = st.TotalBonus;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            row++;
        }

        // Grand total
        row++;
        ws.Cell(row, 1).Value = "GRAND TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = serviceTypeTotals.Sum(x => x.TripCount);
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = Math.Round(serviceTypeTotals.Sum(x => x.TotalHours), 2);
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = serviceTypeTotals.Sum(x => x.TotalBonus);
        ws.Cell(row, 5).Style.Font.Bold = true;
        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
        ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.LightYellow;

        ws.Columns().AdjustToContents();
    }
    
    /// <summary>
    /// Check if trip qualifies for Sierra Nevada MB Sprinter Round Trip exception.
    /// These trips are calculated hourly instead of per-trip.
    /// </summary>
    private bool IsSierraNevadaSpinterRoundTrip(Domain.Entities.Trip trip)
    {
        // Check company name
        var companyMatch = trip.CompanyName?.Contains("Sierra Nevada", StringComparison.OrdinalIgnoreCase) ?? false;
    
        // Check vehicle type (MB Sprinter)
        var vehicleModel = TripQueryExtensions.ParseVehicleModel(trip.ImportedVehiclePlate);
        var vehicleMatch = vehicleModel.Contains("Sprinter", StringComparison.OrdinalIgnoreCase) ||
                           vehicleModel.Contains("MB Sprinter", StringComparison.OrdinalIgnoreCase);
    
        // Check service type (Round Trip)
        var serviceTypeMatch = trip.ServiceType?.Name?.Equals("Round Trip", StringComparison.OrdinalIgnoreCase) ?? false;
    
        return companyMatch && vehicleMatch && serviceTypeMatch;
    }
}