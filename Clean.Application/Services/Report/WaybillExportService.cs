using Clean.Application.Abstractions;
using Clean.Application.Dtos.Report;
using Clean.Domain.Entities;
using ClosedXML.Excel;

namespace Clean.Application.Services.Report;

/// <summary>
/// Exports reports in Russian waybill (путевой лист) format.
/// 
/// CRITICAL: Mileage and fuel are calculated chronologically across ALL trips
/// for a vehicle, then displayed on appropriate pages with correct synced values.
/// 
/// FUEL ALLOCATION: Fuel is allocated TO THE TRIP that needs it - placed at the 
/// optimal point where the tank would otherwise go negative or too low.
/// Fuel appears as a green highlight on the trip row, not as a separate row.
/// </summary>
public class WaybillExportService
{
    private readonly IUnitOfWork _uow;
    private readonly JourneyGroupingService _journeyGroupingService;

    private const int MAX_PAGES_PER_VEHICLE = 2;
    private const int MAX_DRIVERS_PER_PAGE = 2;
    private const double MIN_FUEL_THRESHOLD = 5.0; // Minimum fuel level before we need to allocate

    public WaybillExportService(IUnitOfWork uow)
    {
        _uow = uow;
        _journeyGroupingService = new JourneyGroupingService();
    }

    public async Task<byte[]> ExportWaybillReportAsync(int periodId)
    {
        var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
        if (period == null) return [];

        var vehicles = (await _uow.Vehicles.GetAllAsync()).ToList();
        var vehicleMileages = vehicles.ToDictionary(v => v.Id, v => v.CurrentMileage);
        var fuelAllocations = (await _uow.FuelAllocations.GetByPeriodIdAsync(periodId)).ToList();
        var journeys = _journeyGroupingService.GroupTripsIntoJourneys(period, vehicleMileages);

        using var workbook = new XLWorkbook();

        CreateVehicleSheets(workbook, journeys, fuelAllocations, vehicles, period);
        CreateDetailedSheet(workbook, journeys);
        CreateSummarySheet(workbook, journeys, fuelAllocations, period);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void CreateVehicleSheets(
        IXLWorkbook workbook,
        List<JourneyDto> journeys,
        List<VehicleFuelAllocation> fuelAllocations,
        List<Domain.Entities.Vehicle> vehicles,
        Domain.Entities.ReportPeriod period)
    {
        var journeysByVehicle = journeys
            .GroupBy(j => new { j.VehicleId, j.VehiclePlate, j.VehicleModel })
            .OrderBy(g => g.Key.VehiclePlate)
            .ToList();

        foreach (var vehicleGroup in journeysByVehicle)
        {
            var vehicleId = vehicleGroup.Key.VehicleId;
            var vehiclePlate = vehicleGroup.Key.VehiclePlate;
            var vehicleModel = vehicleGroup.Key.VehicleModel;
            var vehicle = vehicles.FirstOrDefault(v => v.Id == vehicleId);

            var vehicleFuelAllocations = fuelAllocations
                .Where(fa => fa.VehicleId == vehicleId)
                .OrderBy(fa => fa.AllocationDate)
                .ToList();

            var allVehicleJourneys = vehicleGroup.ToList();

            // ================================================================
            // STEP 1: Pre-calculate ALL timeline items with OPTIMALLY PLACED fuel
            // Fuel is allocated to the trip that needs it most
            // ================================================================
            var processedTimeline = PreCalculateVehicleTimelineWithOptimalFuel(
                allVehicleJourneys,
                vehicleFuelAllocations,
                vehicle);

            // Calculate totals
            double totalVehicleFuelConsumed = allVehicleJourneys.Sum(j => j.TotalFuelConsumed);
            double totalVehicleFuelAllocated = vehicleFuelAllocations.Sum(fa => fa.LitersAllocated);
            double totalVehicleDistance = allVehicleJourneys.Sum(j => j.TotalDistanceKm);
            double initialFuelLevel = vehicle?.InitialFuelLevel ?? 0;
            double endingFuelLevel = initialFuelLevel + totalVehicleFuelAllocated - totalVehicleFuelConsumed;

            // Get unique drivers
            var driversList = vehicleGroup
                .GroupBy(j => new { j.DriverId, j.DriverName })
                .OrderBy(g => g.Min(j => j.Date))
                .ThenBy(g => g.Min(j => j.DepartureTime))
                .Select(g => new { g.Key.DriverId, g.Key.DriverName })
                .ToList();

            // Split drivers into pages
            var driverPages = new List<List<(int DriverId, string DriverName)>>();
            for (int i = 0; i < driversList.Count && driverPages.Count < MAX_PAGES_PER_VEHICLE; i += MAX_DRIVERS_PER_PAGE)
            {
                var pageDrivers = driversList
                    .Skip(i)
                    .Take(MAX_DRIVERS_PER_PAGE)
                    .Select(d => (d.DriverId, d.DriverName))
                    .ToList();
                driverPages.Add(pageDrivers);
            }

            int totalPages = driverPages.Count;

            // ================================================================
            // STEP 2: Distribute timeline items to pages based on driver
            // ================================================================
            var timelineByPage = DistributeTimelineToPages(processedTimeline, driverPages);

            // Create sheets for each page
            for (int pageIndex = 0; pageIndex < driverPages.Count; pageIndex++)
            {
                var pageDrivers = driverPages[pageIndex];
                int pageNumber = pageIndex + 1;

                var pageTimeline = timelineByPage.GetValueOrDefault(pageIndex, new List<ProcessedTimelineItem>());

                var driverNamesForHeader = string.Join(", ", pageDrivers.Select(d => d.DriverName));
                string sheetName = CreateSheetName(vehiclePlate, pageNumber, totalPages);

                var ws = workbook.Worksheets.Add(sheetName);

                CreateVehicleHeader(ws, vehiclePlate, vehicleModel, driverNamesForHeader,
                    vehicle, period, pageNumber, totalPages, pageDrivers.Count > 1);

                CreateVehicleJourneyRows(
                    ws,
                    pageTimeline,
                    pageDrivers,
                    totalVehicleFuelConsumed,
                    totalVehicleFuelAllocated,
                    totalVehicleDistance,
                    initialFuelLevel,
                    endingFuelLevel,
                    pageDrivers.Count > 1);
            }
        }
    }

    /// <summary>
    /// Pre-calculate mileage and fuel for ALL journeys, with fuel allocations
    /// optimally placed at the trip that needs them (before tank goes negative).
    /// 
    /// Algorithm:
    /// 1. Sort all journeys chronologically
    /// 2. For each journey, check if fuel would go negative after the trip
    /// 3. If yes, find the best fuel allocation to apply BEFORE/DURING this trip
    /// 4. The fuel allocation is attached to the journey row, not separate
    /// </summary>
    private List<ProcessedTimelineItem> PreCalculateVehicleTimelineWithOptimalFuel(
        List<JourneyDto> allJourneys,
        List<VehicleFuelAllocation> fuelAllocations,
        Domain.Entities.Vehicle? vehicle)
    {
        var timeline = new List<ProcessedTimelineItem>();
        
        // Sort journeys chronologically
        var sortedJourneys = allJourneys
            .OrderBy(j => j.Date)
            .ThenBy(j => j.DepartureTime)
            .ToList();

        // Create a queue of available fuel allocations (sorted by date)
        var availableAllocations = fuelAllocations
            .OrderBy(fa => fa.AllocationDate)
            .ToList();

        // Track which allocations have been used
        var usedAllocationIds = new HashSet<int>();

        // Calculate starting values
        double runningMileage = sortedJourneys.FirstOrDefault()?.StartingMileage 
            ?? vehicle?.CurrentMileage ?? 0;
        double runningFuel = vehicle?.InitialFuelLevel ?? 0;
        double tankCapacity = vehicle?.FuelTankCapacity ?? 60; // Default tank capacity

        // Process each journey
        foreach (var journey in sortedJourneys)
        {
            // Calculate what fuel would be after this trip WITHOUT any allocation
            double fuelAfterTrip = runningFuel - journey.TotalFuelConsumed;
            
            // Find allocations that should be applied to this trip
            // Criteria: 
            // 1. Allocation date is on or before this trip's date
            // 2. Either fuel would go negative/low, OR allocation date matches trip date
            var allocationsForThisTrip = new List<VehicleFuelAllocation>();
            
            foreach (var allocation in availableAllocations)
            {
                if (usedAllocationIds.Contains(allocation.Id))
                    continue;

                // Allocation must be on or before this trip date
                if (allocation.AllocationDate.Date > journey.Date.Date)
                    continue;

                // Check if we need this fuel:
                // 1. Fuel would go below threshold after trip
                // 2. OR allocation is specifically dated for this trip's date
                bool needsFuel = fuelAfterTrip < MIN_FUEL_THRESHOLD;
                bool allocationIsForToday = allocation.AllocationDate.Date == journey.Date.Date;
                
                // Also check: would adding this fuel exceed tank capacity?
                double projectedFuelWithAllocation = runningFuel + allocation.LitersAllocated;
                bool wouldOverfill = projectedFuelWithAllocation > tankCapacity + 5; // 5L tolerance
                
                // Apply allocation if:
                // - We need fuel and this allocation helps, OR
                // - Allocation is dated for today and won't cause major overfill
                if (needsFuel || (allocationIsForToday && !wouldOverfill))
                {
                    // Double-check: if we already have enough fuel and this would overfill, skip
                    if (!needsFuel && wouldOverfill)
                        continue;
                        
                    allocationsForThisTrip.Add(allocation);
                    usedAllocationIds.Add(allocation.Id);
                    
                    // Update projected fuel
                    runningFuel += allocation.LitersAllocated;
                    fuelAfterTrip = runningFuel - journey.TotalFuelConsumed;
                }
            }

            // Calculate fuel values with allocations applied BEFORE the trip
            double fuelBeforeTrip = runningFuel;
            double fuelAddedDuringTrip = allocationsForThisTrip.Sum(a => a.LitersAllocated);
            
            // If fuel would still go negative, apply any remaining same-day allocations
            if (fuelAfterTrip < 0)
            {
                var emergencyAllocations = availableAllocations
                    .Where(a => !usedAllocationIds.Contains(a.Id) && 
                                a.AllocationDate.Date <= journey.Date.Date)
                    .ToList();
                    
                foreach (var allocation in emergencyAllocations)
                {
                    allocationsForThisTrip.Add(allocation);
                    usedAllocationIds.Add(allocation.Id);
                    fuelBeforeTrip += allocation.LitersAllocated;
                    fuelAddedDuringTrip += allocation.LitersAllocated;
                    fuelAfterTrip += allocation.LitersAllocated;
                    
                    if (fuelAfterTrip >= 0)
                        break;
                }
            }

            var processed = new ProcessedTimelineItem
            {
                Type = TimelineItemType.Journey,
                Journey = journey,
                MileageBefore = runningMileage,
                MileageAfter = runningMileage + journey.TotalDistanceKm,
                FuelBefore = fuelBeforeTrip,
                FuelAfter = fuelBeforeTrip - journey.TotalFuelConsumed,
                FuelAddedDuringTrip = fuelAddedDuringTrip,
                AttachedAllocations = allocationsForThisTrip,
                Date = journey.Date,
                Time = journey.DepartureTime
            };

            timeline.Add(processed);

            // Update running values for next iteration
            runningMileage = processed.MileageAfter;
            runningFuel = processed.FuelAfter;
        }

        // Handle any remaining unused allocations - attach to the last journey of matching date
        // or the last journey overall
        var unusedAllocations = availableAllocations
            .Where(a => !usedAllocationIds.Contains(a.Id))
            .ToList();
            
        foreach (var allocation in unusedAllocations)
        {
            // Find the best journey to attach this to
            var targetJourney = timeline
                .Where(t => t.Journey != null && t.Journey.Date.Date == allocation.AllocationDate.Date)
                .LastOrDefault() 
                ?? timeline.LastOrDefault();

            if (targetJourney != null)
            {
                targetJourney.AttachedAllocations.Add(allocation);
                targetJourney.FuelAddedDuringTrip += allocation.LitersAllocated;
                
                // Recalculate fuel for this and subsequent items
                RecalculateFuelFromIndex(timeline, timeline.IndexOf(targetJourney));
            }
        }

        return timeline;
    }

    /// <summary>
    /// Recalculate fuel levels from a given index onwards after adding an allocation
    /// </summary>
    private void RecalculateFuelFromIndex(List<ProcessedTimelineItem> timeline, int startIndex)
    {
        if (startIndex < 0 || startIndex >= timeline.Count)
            return;

        for (int i = startIndex; i < timeline.Count; i++)
        {
            var item = timeline[i];
            
            if (i == startIndex)
            {
                // For the modified item, add the allocation to fuel before
                double previousFuel = i > 0 ? timeline[i - 1].FuelAfter : item.FuelBefore;
                item.FuelBefore = previousFuel + item.FuelAddedDuringTrip;
                item.FuelAfter = item.FuelBefore - (item.Journey?.TotalFuelConsumed ?? 0);
            }
            else
            {
                // For subsequent items, chain the fuel values
                item.FuelBefore = timeline[i - 1].FuelAfter + item.FuelAddedDuringTrip;
                item.FuelAfter = item.FuelBefore - (item.Journey?.TotalFuelConsumed ?? 0);
            }
        }
    }

    /// <summary>
    /// Distribute pre-calculated timeline items to pages.
    /// Journeys go to their driver's page.
    /// </summary>
    private Dictionary<int, List<ProcessedTimelineItem>> DistributeTimelineToPages(
        List<ProcessedTimelineItem> processedTimeline,
        List<List<(int DriverId, string DriverName)>> driverPages)
    {
        var result = new Dictionary<int, List<ProcessedTimelineItem>>();
        for (int i = 0; i < driverPages.Count; i++)
        {
            result[i] = new List<ProcessedTimelineItem>();
        }

        // Build driver to page mapping
        var driverToPage = new Dictionary<int, int>();
        for (int pageIndex = 0; pageIndex < driverPages.Count; pageIndex++)
        {
            foreach (var (driverId, _) in driverPages[pageIndex])
            {
                driverToPage[driverId] = pageIndex;
            }
        }

        foreach (var item in processedTimeline)
        {
            if (item.Journey != null)
            {
                var driverId = item.Journey.DriverId;
                if (driverToPage.TryGetValue(driverId, out int pageIndex))
                {
                    result[pageIndex].Add(item);
                }
            }
        }

        // Sort each page by date/time
        foreach (var page in result.Values)
        {
            page.Sort((a, b) =>
            {
                int dateCompare = a.Date.CompareTo(b.Date);
                return dateCompare != 0 ? dateCompare : a.Time.CompareTo(b.Time);
            });
        }

        return result;
    }

    private string CreateSheetName(string vehiclePlate, int pageNumber, int totalPages)
    {
        var cleanPlate = vehiclePlate
            .Replace("/", "-").Replace("\\", "-").Replace("*", "")
            .Replace("?", "").Replace("[", "").Replace("]", "").Replace(":", "");

        string sheetName = totalPages > 1 ? $"{cleanPlate} - {pageNumber}" : cleanPlate;
        return sheetName.Length > 31 ? sheetName.Substring(0, 31) : sheetName;
    }

    private void CreateVehicleHeader(
        IXLWorksheet ws, string vehiclePlate, string vehicleModel, string driverNames,
        Domain.Entities.Vehicle? vehicle, Domain.Entities.ReportPeriod period, int pageNumber, int totalPages, bool multipleDrivers)
    {
        ws.Cell(1, 1).Value = "ПУТЕВОЙ ЛИСТ";
        ws.Range(1, 1, 1, 15).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        string pageInfo = totalPages > 1 ? $" (Лист {pageNumber} из {totalPages})" : "";
        ws.Cell(2, 1).Value = $"Период: {period.StartDate:dd.MM.yyyy} - {period.EndDate:dd.MM.yyyy}{pageInfo}";
        ws.Range(2, 1, 2, 15).Merge();
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(4, 1).Value = "Автомобиль:";
        ws.Cell(4, 1).Style.Font.Bold = true;
        ws.Cell(4, 2).Value = vehicleModel;

        ws.Cell(4, 4).Value = "Гос. номер:";
        ws.Cell(4, 4).Style.Font.Bold = true;
        ws.Cell(4, 5).Value = vehiclePlate;

        ws.Cell(5, 1).Value = multipleDrivers ? "Водители:" : "Водитель:";
        ws.Cell(5, 1).Style.Font.Bold = true;
        ws.Cell(5, 2).Value = driverNames;
        ws.Range(5, 2, 5, 5).Merge();

        if (vehicle != null)
        {
            ws.Cell(6, 1).Value = "Тип топлива:";
            ws.Cell(6, 1).Style.Font.Bold = true;
            ws.Cell(6, 2).Value = vehicle.FuelType ?? "Н/Д";

            ws.Cell(6, 4).Value = "Расход л/100км:";
            ws.Cell(6, 4).Style.Font.Bold = true;
            ws.Cell(6, 5).Value = vehicle.FuelConsumptionPer100Km;

            ws.Cell(4, 7).Value = "Нач. топливо:";
            ws.Cell(4, 7).Style.Font.Bold = true;
            ws.Cell(4, 8).Value = $"{vehicle.InitialFuelLevel:F1} л";
            
            ws.Cell(5, 7).Value = "Объём бака:";
            ws.Cell(5, 7).Style.Font.Bold = true;
            ws.Cell(5, 8).Value = $"{vehicle.FuelTankCapacity} л";
        }

        ws.Range(4, 1, 6, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    private void CreateVehicleJourneyRows(
        IXLWorksheet ws,
        List<ProcessedTimelineItem> pageTimeline,
        List<(int DriverId, string DriverName)> pageDrivers,
        double totalVehicleFuelConsumed,
        double totalVehicleFuelAllocated,
        double totalVehicleDistance,
        double initialFuelLevel,
        double endingFuelLevel,
        bool showDriverColumn)
    {
        int headerRow = 8;

        // Updated headers - "Заправка (л)" is now integrated into trip rows
        var headersList = new List<string> { "№", "Дата", "Выезд", "Возврат" };
        if (showDriverColumn) headersList.Add("Водитель");
        headersList.AddRange(new[] {
            "Компания", "Заказы",
            "Спидометр выезд", "Спидометр возврат", "Пробег (км)",
            "Расход (л)", "Заправка (л)",
            "Топливо до", "Топливо после",
            "Примечание"
        });

        var headers = headersList.ToArray();
        int colCount = headers.Length;

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(headerRow, i + 1).Value = headers[i];
            ws.Cell(headerRow, i + 1).Style.Font.Bold = true;
            ws.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Cell(headerRow, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(headerRow, i + 1).Style.Alignment.WrapText = true;
        }

        ws.Range(headerRow, 1, headerRow, colCount).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(headerRow, 1, headerRow, colCount).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        int dataRow = headerRow + 1;
        int journeyNumber = 1;

        double pageTotalDistance = 0;
        double pageTotalFuelConsumed = 0;
        double pageTotalFuelAdded = 0;

        var driverStats = new Dictionary<int, (string Name, double Distance, double Fuel, double FuelAdded, int Journeys)>();

        foreach (var item in pageTimeline)
        {
            var journey = item.Journey!;
            int col = 1;

            ws.Cell(dataRow, col++).Value = journeyNumber++;
            ws.Cell(dataRow, col++).Value = journey.Date.ToString("dd.MM.yyyy");
            ws.Cell(dataRow, col++).Value = journey.DepartureTime.ToString(@"hh\:mm");
            ws.Cell(dataRow, col++).Value = journey.ReturnTime.ToString(@"hh\:mm");

            if (showDriverColumn)
                ws.Cell(dataRow, col++).Value = journey.DriverName;

            ws.Cell(dataRow, col++).Value = journey.Companies;
            ws.Cell(dataRow, col++).Value = string.Join(", ", journey.ConfNumbers);

            int mileageCol = col;
            ws.Cell(dataRow, col++).Value = Math.Round(item.MileageBefore, 0);
            ws.Cell(dataRow, col++).Value = Math.Round(item.MileageAfter, 0);
            ws.Cell(dataRow, col++).Value = Math.Round(journey.TotalDistanceKm, 1);
            ws.Cell(dataRow, col++).Value = Math.Round(journey.TotalFuelConsumed, 2);
            
            // Fuel added column - shows fuel allocated to this trip
            int fuelAddedCol = col;
            if (item.FuelAddedDuringTrip > 0)
            {
                ws.Cell(dataRow, col).Value = Math.Round(item.FuelAddedDuringTrip, 2);
            }
            col++;

            int fuelBeforeCol = col;
            ws.Cell(dataRow, col++).Value = Math.Round(item.FuelBefore, 2);
            ws.Cell(dataRow, col++).Value = Math.Round(item.FuelAfter, 2);

            // Build notes
            var notes = new List<string>();
            if (journey.TripCount > 1)
                notes.Add($"{journey.TripCount} работ");
            
            // Check if any trip in the journey is a Field Trip
            if (journey.Trips.Any(t => t.ServiceType?.Equals("Field Trip", StringComparison.OrdinalIgnoreCase) == true))
                notes.Add("Field Trip");
            
            // Add fuel allocation reasons if any
            if (item.AttachedAllocations.Any())
            {
                var reasons = item.AttachedAllocations
                    .Select(a => GetAllocationReasonText(a.Reason))
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Distinct();
                if (reasons.Any())
                    notes.Add($"⛽ {string.Join(", ", reasons)}");
            }
            
            ws.Cell(dataRow, col++).Value = string.Join(", ", notes);

            pageTotalDistance += journey.TotalDistanceKm;
            pageTotalFuelConsumed += journey.TotalFuelConsumed;
            pageTotalFuelAdded += item.FuelAddedDuringTrip;

            if (!driverStats.ContainsKey(journey.DriverId))
                driverStats[journey.DriverId] = (journey.DriverName, 0, 0, 0, 0);
            var ds = driverStats[journey.DriverId];
            driverStats[journey.DriverId] = (ds.Name, ds.Distance + journey.TotalDistanceKm,
                ds.Fuel + journey.TotalFuelConsumed, ds.FuelAdded + item.FuelAddedDuringTrip, ds.Journeys + 1);

            // Style
            var rowRange = ws.Range(dataRow, 1, dataRow, colCount);
            rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Cell(dataRow, mileageCol).Style.NumberFormat.Format = "#,##0";
            ws.Cell(dataRow, mileageCol + 1).Style.NumberFormat.Format = "#,##0";
            ws.Cell(dataRow, mileageCol + 2).Style.NumberFormat.Format = "#,##0.0";
            ws.Cell(dataRow, mileageCol + 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(dataRow, fuelAddedCol).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(dataRow, fuelBeforeCol).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(dataRow, fuelBeforeCol + 1).Style.NumberFormat.Format = "#,##0.00";

            // Highlight row with GREEN if fuel was added during this trip
            if (item.FuelAddedDuringTrip > 0)
            {
                rowRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
                ws.Cell(dataRow, fuelAddedCol).Style.Font.Bold = true;
                ws.Cell(dataRow, fuelAddedCol).Style.Font.FontColor = XLColor.DarkGreen;
            }

            // Warning if fuel is low after trip
            if (item.FuelAfter < 10)
            {
                ws.Cell(dataRow, fuelBeforeCol + 1).Style.Font.FontColor = XLColor.Red;
                ws.Cell(dataRow, fuelBeforeCol + 1).Style.Font.Bold = true;
            }
            
            // Warning if fuel went negative (should be rare now)
            if (item.FuelAfter < 0)
            {
                ws.Cell(dataRow, fuelBeforeCol + 1).Style.Fill.BackgroundColor = XLColor.LightCoral;
            }

            dataRow++;
        }

        // Column positions for totals
        int distanceCol = showDriverColumn ? 10 : 9;
        int mergeEndCol = distanceCol - 1;
        int fuelConsumedCol = distanceCol + 1;
        int fuelAddedTotalCol = distanceCol + 2;
        int fuelBeforeTotalCol = distanceCol + 3;

        // Per-driver subtotals
        if (showDriverColumn && driverStats.Count > 1)
        {
            dataRow++;
            foreach (var kvp in driverStats.OrderBy(x => x.Value.Name))
            {
                var ds = kvp.Value;
                ws.Cell(dataRow, 1).Value = $"Итого {ds.Name}:";
                ws.Range(dataRow, 1, dataRow, mergeEndCol).Merge();
                ws.Cell(dataRow, 1).Style.Font.Bold = true;
                ws.Cell(dataRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(dataRow, distanceCol).Value = Math.Round(ds.Distance, 1);
                ws.Cell(dataRow, fuelConsumedCol).Value = Math.Round(ds.Fuel, 2);
                if (ds.FuelAdded > 0)
                    ws.Cell(dataRow, fuelAddedTotalCol).Value = Math.Round(ds.FuelAdded, 2);

                ws.Range(dataRow, 1, dataRow, colCount).Style.Fill.BackgroundColor = XLColor.LightCyan;
                ws.Range(dataRow, 1, dataRow, colCount).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                dataRow++;
            }
        }

        // Page subtotal
        dataRow++;
        ws.Cell(dataRow, 1).Value = "Итого по листу:";
        ws.Range(dataRow, 1, dataRow, mergeEndCol).Merge();
        ws.Cell(dataRow, 1).Style.Font.Bold = true;
        ws.Cell(dataRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(dataRow, distanceCol).Value = Math.Round(pageTotalDistance, 1);
        ws.Cell(dataRow, fuelConsumedCol).Value = Math.Round(pageTotalFuelConsumed, 2);
        if (pageTotalFuelAdded > 0)
            ws.Cell(dataRow, fuelAddedTotalCol).Value = Math.Round(pageTotalFuelAdded, 2);

        ws.Range(dataRow, 1, dataRow, colCount).Style.Font.Bold = true;
        ws.Range(dataRow, 1, dataRow, colCount).Style.Fill.BackgroundColor = XLColor.LightBlue;
        ws.Range(dataRow, 1, dataRow, colCount).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // VEHICLE TOTAL
        dataRow += 2;
        ws.Cell(dataRow, 1).Value = "ИТОГО ПО АВТОМОБИЛЮ:";
        ws.Range(dataRow, 1, dataRow, mergeEndCol).Merge();
        ws.Cell(dataRow, 1).Style.Font.Bold = true;
        ws.Cell(dataRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(dataRow, distanceCol).Value = Math.Round(totalVehicleDistance, 1);
        ws.Cell(dataRow, fuelConsumedCol).Value = Math.Round(totalVehicleFuelConsumed, 2);
        ws.Cell(dataRow, fuelAddedTotalCol).Value = Math.Round(totalVehicleFuelAllocated, 2);

        ws.Range(dataRow, 1, dataRow, colCount).Style.Font.Bold = true;
        ws.Range(dataRow, 1, dataRow, colCount).Style.Fill.BackgroundColor = XLColor.LightYellow;
        ws.Range(dataRow, 1, dataRow, colCount).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Fuel balance
        dataRow++;
        double vehicleFuelBalance = totalVehicleFuelAllocated - totalVehicleFuelConsumed;
        ws.Cell(dataRow, 1).Value = "Баланс топлива (заправлено - израсходовано):";
        ws.Range(dataRow, 1, dataRow, fuelConsumedCol).Merge();
        ws.Cell(dataRow, 1).Style.Font.Bold = true;
        ws.Cell(dataRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(dataRow, fuelAddedTotalCol).Value = Math.Round(vehicleFuelBalance, 2);
        ws.Cell(dataRow, fuelAddedTotalCol).Style.Font.Bold = true;
        ws.Cell(dataRow, fuelAddedTotalCol).Style.NumberFormat.Format = "+#,##0.00;-#,##0.00;0";

        if (vehicleFuelBalance < 0)
        {
            ws.Cell(dataRow, fuelAddedTotalCol).Style.Font.FontColor = XLColor.Red;
            ws.Cell(dataRow, fuelBeforeTotalCol).Value = "ДЕФИЦИТ";
            ws.Cell(dataRow, fuelBeforeTotalCol).Style.Font.FontColor = XLColor.Red;
        }
        else
        {
            ws.Cell(dataRow, fuelAddedTotalCol).Style.Font.FontColor = XLColor.DarkGreen;
        }

        // ENDING FUEL LEVEL
        dataRow++;
        ws.Cell(dataRow, 1).Value = "ОСТАТОК ТОПЛИВА В БАКЕ (на конец периода):";
        ws.Range(dataRow, 1, dataRow, fuelConsumedCol).Merge();
        ws.Cell(dataRow, 1).Style.Font.Bold = true;
        ws.Cell(dataRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(dataRow, fuelAddedTotalCol).Value = Math.Round(Math.Max(0, endingFuelLevel), 2);
        ws.Cell(dataRow, fuelAddedTotalCol).Style.Font.Bold = true;
        ws.Cell(dataRow, fuelAddedTotalCol).Style.Font.FontSize = 12;
        ws.Cell(dataRow, fuelBeforeTotalCol).Value = "литров";

        ws.Range(dataRow, 1, dataRow, colCount).Style.Fill.BackgroundColor = XLColor.LightGoldenrodYellow;
        ws.Range(dataRow, 1, dataRow, colCount).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

        if (endingFuelLevel < 0)
        {
            ws.Cell(dataRow, fuelAddedTotalCol).Style.Font.FontColor = XLColor.Red;
            ws.Cell(dataRow, fuelBeforeTotalCol).Value = "л (ВНИМАНИЕ!)";
            ws.Cell(dataRow, fuelBeforeTotalCol).Style.Font.FontColor = XLColor.Red;
        }

        // Signatures
        dataRow += 3;
        ws.Cell(dataRow, 1).Value = "Отметка мед. работника: ________________";
        ws.Range(dataRow, 1, dataRow, 4).Merge();
        ws.Cell(dataRow, 7).Value = "Выезд разрешил: ________________";
        ws.Range(dataRow, 7, dataRow, 10).Merge();

        dataRow += 2;
        ws.Cell(dataRow, 1).Value = "Подпись водителя: ________________";
        ws.Range(dataRow, 1, dataRow, 4).Merge();
        ws.Cell(dataRow, 7).Value = "АТС принял: ________________";
        ws.Range(dataRow, 7, dataRow, 10).Merge();

        SetColumnWidths(ws, showDriverColumn);
    }

    private void SetColumnWidths(IXLWorksheet ws, bool hasDriverColumn)
    {
        int col = 1;
        ws.Column(col++).Width = 5;
        ws.Column(col++).Width = 11;
        ws.Column(col++).Width = 7;
        ws.Column(col++).Width = 7;
        if (hasDriverColumn) ws.Column(col++).Width = 15;
        ws.Column(col++).Width = 18;
        ws.Column(col++).Width = 14;
        ws.Column(col++).Width = 10;
        ws.Column(col++).Width = 10;
        ws.Column(col++).Width = 8;
        ws.Column(col++).Width = 9;
        ws.Column(col++).Width = 9;
        ws.Column(col++).Width = 9;
        ws.Column(col++).Width = 9;
        ws.Column(col++).Width = 14;
    }

    private string GetAllocationReasonText(FuelAllocationReason reason) => reason switch
    {
        FuelAllocationReason.AutoDistanceBased => "Авто",
        FuelAllocationReason.ManualAllocation => "Ручная",
        FuelAllocationReason.BalanceAdjustment => "Корр.",
        FuelAllocationReason.InitialFillUp => "Начальная",
        FuelAllocationReason.PeriodCarryOver => "Перенос",
        _ => ""
    };

    // ========== DETAILED SHEET ==========
    private void CreateDetailedSheet(IXLWorkbook workbook, List<JourneyDto> journeys)
    {
        var ws = workbook.Worksheets.Add("Детализация");
        var headers = new[] { "№", "Дата", "Водитель", "Гос. номер", "№ заказа",
            "Выезд", "Заезд", "Компания", "Маршрут", "Тип услуги", "Пробег (км)" };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var j in journeys.OrderBy(j => j.VehiclePlate).ThenBy(j => j.Date).ThenBy(j => j.DepartureTime))
        {
            foreach (var t in j.Trips)
            {
                ws.Cell(row, 1).Value = j.JourneyNumber;
                ws.Cell(row, 2).Value = j.Date.ToString("dd.MM.yyyy");
                ws.Cell(row, 3).Value = j.DriverName;
                ws.Cell(row, 4).Value = j.VehiclePlate;
                ws.Cell(row, 5).Value = t.ConfNumber;
                ws.Cell(row, 6).Value = t.GarageOutTime.ToString(@"hh\:mm");
                ws.Cell(row, 7).Value = t.GarageInTime.ToString(@"hh\:mm");
                ws.Cell(row, 8).Value = t.CompanyName;
                ws.Cell(row, 9).Value = t.RoutingDetails;
                ws.Cell(row, 10).Value = t.ServiceType;
                ws.Cell(row, 11).Value = t.DistanceKm ?? 0;
                row++;
            }
        }
        ws.Columns().AdjustToContents();
    }

    // ========== SUMMARY SHEET ==========
    private void CreateSummarySheet(IXLWorkbook workbook, List<JourneyDto> journeys,
        List<VehicleFuelAllocation> fuelAllocations, Domain.Entities.ReportPeriod period)
    {
        var ws = workbook.Worksheets.Add("Сводка");

        ws.Cell(1, 1).Value = $"Период: {period.StartDate:dd.MM.yyyy} - {period.EndDate:dd.MM.yyyy}";
        ws.Cell(1, 1).Style.Font.Bold = true;

        double totalFuel = fuelAllocations.Sum(fa => fa.LitersAllocated);
        double totalConsumed = journeys.Sum(j => j.TotalFuelConsumed);

        ws.Cell(3, 1).Value = "Выездов:"; ws.Cell(3, 2).Value = journeys.Count;
        ws.Cell(4, 1).Value = "Пробег:"; ws.Cell(4, 2).Value = Math.Round(journeys.Sum(j => j.TotalDistanceKm), 1);
        ws.Cell(5, 1).Value = "Расход:"; ws.Cell(5, 2).Value = Math.Round(totalConsumed, 2);
        ws.Cell(6, 1).Value = "Заправлено:"; ws.Cell(6, 2).Value = Math.Round(totalFuel, 2);
        ws.Cell(7, 1).Value = "Баланс:"; ws.Cell(7, 2).Value = Math.Round(totalFuel - totalConsumed, 2);

        ws.Columns().AdjustToContents();
    }

    public async Task<List<JourneyDto>> GetJourneysAsync(int periodId)
    {
        var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
        if (period == null) return new List<JourneyDto>();
        var vehicles = await _uow.Vehicles.GetAllAsync();
        return _journeyGroupingService.GroupTripsIntoJourneys(period, vehicles.ToDictionary(v => v.Id, v => v.CurrentMileage));
    }

    // Helper classes
    private class ProcessedTimelineItem
    {
        public TimelineItemType Type { get; set; }
        public JourneyDto? Journey { get; set; }
        public double MileageBefore { get; set; }
        public double MileageAfter { get; set; }
        public double FuelBefore { get; set; }
        public double FuelAfter { get; set; }
        public double FuelAddedDuringTrip { get; set; } = 0;
        public List<VehicleFuelAllocation> AttachedAllocations { get; set; } = new();
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
    }

    private enum TimelineItemType { Journey, FuelAllocation }
}