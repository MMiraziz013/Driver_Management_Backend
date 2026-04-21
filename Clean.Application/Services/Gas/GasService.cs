using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Fuel;
using Clean.Application.Dtos.Responses;
using Clean.Application.Services.Report.Finalization;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using MiniExcelLibs;

namespace Clean.Application.Services.Gas;

/// <summary>
/// Service for managing gas/fuel purchases, allocations, and calculations
/// Updated to use repositories via UnitOfWork
/// </summary>
public class GasService : IGasService
{
    private readonly IUnitOfWork _uow;

    // Fuel type constants (Cyrillic)
    private const string FUEL_AI92 = "АИ-92";
    private const string FUEL_AI95 = "АИ-95";
    private const string FUEL_DIESEL = "ДТ";

    // Safety thresholds
    private const double MIN_FUEL_RESERVE = 5.0;
    private const double ALLOCATION_TOLERANCE = 0.01;

    public GasService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    #region Gas Purchase Management

public async Task<Response<GasPurchaseSummaryDto>> UploadGasPurchasesAsync(IFormFile file, int periodId)
    {
        try
        {
            var period = await _uow.ReportPeriods.GetByIdAsync(periodId);
            if (period == null)
                return new Response<GasPurchaseSummaryDto>(HttpStatusCode.NotFound, "Report period not found.");

            // Delete existing purchases and allocations for this period
            var existingPurchases = await _uow.GasPurchases.GetByPeriodIdAsync(periodId);

            if (existingPurchases.Any())
            {
                var purchaseIds = existingPurchases.Select(p => p.Id).ToList();
                
                // Delete allocations for these purchases
                foreach (var purchaseId in purchaseIds)
                {
                    var allocations = await _uow.FuelAllocations.GetByPurchaseIdAsync(purchaseId);
                    if (allocations.Any())
                        _uow.FuelAllocations.RemoveRange(allocations);
                }

                _uow.GasPurchases.RemoveRange(existingPurchases);
                await _uow.CompleteAsync();
            }

            // Parse and upload new purchases
            await using var stream = file.OpenReadStream();
            var rows = await stream.QueryAsync(useHeaderRow: true);

            var newPurchases = new List<GasPurchase>();
            int rowNumber = 1;
            var errors = new List<string>();

            foreach (var row in rows)
            {
                rowNumber++;
                try
                {
                    // Parse Date
                    var dateStr = row.Дата?.ToString() ?? row.Date?.ToString();

                    if (string.IsNullOrWhiteSpace(dateStr))
                    {
                        errors.Add($"Row {rowNumber}: Missing date");
                        continue;
                    }

                    if (!DateTime.TryParse(dateStr, out DateTime parsedDate))
                    {
                        errors.Add($"Row {rowNumber}: Invalid date format '{dateStr}'");
                        continue;
                    }

                    // Parse Gas (liters)
                    double liters = 0.0;
                    var gasValue = row.литр?.ToString() ?? row.Литр?.ToString() ?? row.Liters?.ToString() ?? row.Gas?.ToString();
                    if (string.IsNullOrWhiteSpace(gasValue) || !double.TryParse(gasValue.Replace(",", "."),
                            NumberStyles.Any, CultureInfo.InvariantCulture, out liters))
                    {
                        errors.Add($"Row {rowNumber}: Invalid or missing gas amount");
                        continue;
                    }

                    // Parse Fuel Type - GET THE ORIGINAL VALUE
                    var rawFuelType = row.марка?.ToString() ?? row.Марка?.ToString() ?? 
                                      row.Type?.ToString() ?? row.Тип?.ToString() ?? "";
                    
                    if (string.IsNullOrWhiteSpace(rawFuelType))
                    {
                        errors.Add($"Row {rowNumber}: Invalid or missing fuel type");
                        continue;
                    }

                    // ========================================================
                    // KEY CHANGE: Store BOTH specific and generic fuel types
                    // ========================================================
                    var specificFuelType = FuelTypeHelper.NormalizeSpecificFuelType(rawFuelType);
                    var genericFuelType = FuelTypeHelper.GetGenericFuelType(rawFuelType);

                    // Parse Amount (UZS)
                    decimal amount = 0m;
                    var amountValue = row.Сумма?.ToString() ?? row.сумма?.ToString() ?? row.Amount?.ToString();
                    if (string.IsNullOrWhiteSpace(amountValue) ||
                        !decimal.TryParse(amountValue.Replace(" ", "").Replace(",", ""), out amount))
                    {
                        errors.Add($"Row {rowNumber}: Invalid or missing amount");
                        continue;
                    }

                    var purchase = new GasPurchase
                    {
                        ReportPeriodId = periodId,
                        PurchaseDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc),
                        LitersAmount = liters,
                        SpecificFuelType = specificFuelType,  // "АИ-92", "АИ-95", "ДТ" - for REPORTING
                        FuelType = genericFuelType,            // "Gasoline/АИ", "Diesel/ДТ" - for ALLOCATION
                        AmountUzs = amount,
                        AllocatedLiters = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    newPurchases.Add(purchase);
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {rowNumber}: {ex.Message}");
                }
            }

            if (!newPurchases.Any())
            {
                return new Response<GasPurchaseSummaryDto>(HttpStatusCode.BadRequest,
                    errors.Any() ? errors : new List<string> { "No valid gas purchase records found in file" });
            }

            await _uow.GasPurchases.AddRangeAsync(newPurchases);
            await _uow.CompleteAsync();

            var summary = BuildPurchaseSummary(periodId, newPurchases);

            if (errors.Any())
            {
                summary.Messages = errors.Take(10).ToList();
                if (errors.Count > 10)
                    summary.Messages.Add($"... and {errors.Count - 10} more errors");
            }

            Console.WriteLine($"✓ Uploaded {newPurchases.Count} gas purchases for period {periodId}");
            return new Response<GasPurchaseSummaryDto>(HttpStatusCode.OK, 
                $"Successfully uploaded {newPurchases.Count} gas purchase records", summary);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Gas upload error: {ex.Message}");
            return new Response<GasPurchaseSummaryDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message, ex.InnerException?.Message ?? "" });
        }
    }

    public async Task<Response<List<GasPurchaseDto>>> GetGasPurchasesAsync(int periodId)
    {
        try
        {
            var purchases = await _uow.GasPurchases.GetByPeriodIdAsync(periodId);

            var dtos = purchases.Select(g => new GasPurchaseDto
            {
                Id = g.Id,
                PurchaseDate = g.PurchaseDate,
                LitersAmount = g.LitersAmount,
                FuelType = g.FuelType,                       // Generic type for allocation info
                SpecificFuelType = g.SpecificFuelType,       // Specific type for display
                AmountUzs = g.AmountUzs,
                PricePerLiter = g.PricePerLiter,
                AllocatedLiters = g.AllocatedLiters,
                RemainingLiters = g.RemainingLiters,
                IsFullyAllocated = g.IsFullyAllocated,
                Notes = g.Notes
            }).ToList();

            return new Response<List<GasPurchaseDto>>(HttpStatusCode.OK, dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GasPurchaseDto>>(HttpStatusCode.InternalServerError,
                [ex.Message]);
        }
    }


    public async Task<Response<GasPurchaseSummaryDto>> GetGasPurchaseSummaryAsync(int periodId)
    {
        try
        {
            var purchases = await _uow.GasPurchases.GetByPeriodIdAsync(periodId);
            var summary = BuildPurchaseSummary(periodId, purchases);
            return new Response<GasPurchaseSummaryDto>(HttpStatusCode.OK, summary);
        }
        catch (Exception ex)
        {
            return new Response<GasPurchaseSummaryDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<string>> DeleteGasPurchasesAsync(int periodId)
    {
        try
        {
            var purchases = await _uow.GasPurchases.GetByPeriodIdAsync(periodId);

            if (!purchases.Any())
                return new Response<string>(HttpStatusCode.OK, "No purchases to delete", "Success");

            // Delete allocations first
            var allocations = await _uow.FuelAllocations.GetByPeriodIdAsync(periodId);
            if (allocations.Any())
                _uow.FuelAllocations.RemoveRange(allocations);

            _uow.GasPurchases.RemoveRange(purchases);
            await _uow.CompleteAsync();

            return new Response<string>(HttpStatusCode.OK,
                $"Deleted {purchases.Count} purchases and {allocations.Count} allocations", "Success");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    #endregion

    #region Vehicle Fuel Configuration

    public async Task<Response<string>> UpdateVehicleFuelConfigAsync(UpdateVehicleFuelConfigRequest request)
    {
        try
        {
            var vehicle = await _uow.Vehicles.GetByIdAsync(request.VehicleId);
            if (vehicle == null)
                return new Response<string>(HttpStatusCode.NotFound, "Vehicle not found");

            vehicle.FuelTankCapacity = request.FuelTankCapacity;
            vehicle.FuelConsumptionPer100Km = request.FuelConsumptionPer100Km;
            vehicle.FuelType = NormalizeFuelType(request.FuelType);

            if (request.InitialFuelLevel.HasValue)
                vehicle.InitialFuelLevel = request.InitialFuelLevel.Value;

            vehicle.UpdatedAt = DateTime.UtcNow;
            await _uow.CompleteAsync();

            return new Response<string>(HttpStatusCode.OK,
                $"Updated fuel config for {vehicle.PlateNumber}", "Success");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<string>> BulkUpdateVehicleFuelConfigAsync(List<UpdateVehicleFuelConfigRequest> requests)
    {
        try
        {
            int updated = 0;
            var errors = new List<string>();

            foreach (var request in requests)
            {
                var vehicle = await _uow.Vehicles.GetByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    errors.Add($"Vehicle ID {request.VehicleId} not found");
                    continue;
                }

                vehicle.FuelTankCapacity = request.FuelTankCapacity;
                vehicle.FuelConsumptionPer100Km = request.FuelConsumptionPer100Km;
                vehicle.FuelType = NormalizeFuelType(request.FuelType);

                if (request.InitialFuelLevel.HasValue)
                    vehicle.InitialFuelLevel = request.InitialFuelLevel.Value;

                vehicle.UpdatedAt = DateTime.UtcNow;
                updated++;
            }

            await _uow.CompleteAsync();

            var message = $"Updated {updated} vehicles";
            if (errors.Any())
                message += $", {errors.Count} errors";

            return new Response<string>(HttpStatusCode.OK, message, "Success");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<List<VehicleFuelStatusDto>>> GetVehicleFuelConfigsAsync()
    {
        try
        {
            var vehicles = await _uow.Vehicles.GetAllAsync();

            var configs = vehicles.Select(v => new VehicleFuelStatusDto
            {
                VehicleId = v.Id,
                PlateNumber = v.PlateNumber,
                Model = v.Model,
                FuelType = v.FuelType,
                TankCapacity = v.FuelTankCapacity,
                ConsumptionPer100Km = v.FuelConsumptionPer100Km,
                InitialFuelLevel = v.InitialFuelLevel,
                Status = string.IsNullOrEmpty(v.FuelType) ? "NOT_CONFIGURED" : "CONFIGURED"
            }).ToList();

            return new Response<List<VehicleFuelStatusDto>>(HttpStatusCode.OK, configs);
        }
        catch (Exception ex)
        {
            return new Response<List<VehicleFuelStatusDto>>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<string>> SetInitialFuelLevelAsync(SetInitialFuelLevelRequest request)
    {
        try
        {
            var vehicle = await _uow.Vehicles.GetByIdAsync(request.VehicleId);
            if (vehicle == null)
                return new Response<string>(HttpStatusCode.NotFound, "Vehicle not found");

            if (request.InitialLiters > vehicle.FuelTankCapacity && vehicle.FuelTankCapacity > 0)
                return new Response<string>(HttpStatusCode.BadRequest,
                    $"Initial level ({request.InitialLiters}L) exceeds tank capacity ({vehicle.FuelTankCapacity}L)");

            vehicle.InitialFuelLevel = request.InitialLiters;
            vehicle.UpdatedAt = DateTime.UtcNow;
            await _uow.CompleteAsync();

            return new Response<string>(HttpStatusCode.OK,
                $"Set initial fuel level to {request.InitialLiters}L for {vehicle.PlateNumber}", "Success");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    #endregion

    #region Fuel Allocation & Calculation

    public async Task<Response<FuelCalculationResultDto>> RunAutoFuelAllocationAsync(int periodId)
        {
            try
            {
                Console.WriteLine($"\n{'=',-60}");
                Console.WriteLine($"=== STARTING DAILY FUEL ALLOCATION FOR PERIOD {periodId} ===");
                Console.WriteLine($"{'=',-60}\n");

                var result = new FuelCalculationResultDto
                {
                    ReportPeriodId = periodId,
                    CalculatedAt = DateTime.UtcNow,
                    Success = true
                };

                // 1. Get all data needed
                var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
                if (period == null)
                {
                    result.Success = false;
                    result.Errors.Add("Report period not found");
                    return new Response<FuelCalculationResultDto>(HttpStatusCode.NotFound, result);
                }

                var vehicles = await _uow.Vehicles.GetAllAsync();

                // 2. Delete existing allocations and reset purchases
                var existingAllocations = await _uow.FuelAllocations.GetByPeriodIdAsync(periodId);
                if (existingAllocations.Any())
                {
                    _uow.FuelAllocations.RemoveRange(existingAllocations);
                    Console.WriteLine($"✓ Clearing {existingAllocations.Count} existing allocations");
                }

                var gasPurchases = await _uow.GasPurchases.GetByPeriodIdAsync(periodId);

                if (!gasPurchases.Any())
                {
                    result.Success = false;
                    result.Errors.Add("No gas purchases found for this period. Please upload gas report first.");
                    return new Response<FuelCalculationResultDto>(HttpStatusCode.BadRequest, result);
                }

                // Reset all allocated liters
                foreach (var purchase in gasPurchases)
                {
                    purchase.AllocatedLiters = 0;
                }

                await _uow.CompleteAsync();

                // 3. Build vehicle fuel tracking objects
                var vehicleFuelTrackers = new Dictionary<int, VehicleFuelTracker>();

                foreach (var vehicle in vehicles.Where(v =>
                             !string.IsNullOrEmpty(v.FuelType) &&
                             !v.FuelType.Equals("Electric", StringComparison.OrdinalIgnoreCase) &&
                             !v.FuelType.Equals("Электро", StringComparison.OrdinalIgnoreCase)))
                {
                    var vehicleTrips = period.Trips
                        .Where(t => t.Assignments.Any(a => a.VehicleId == vehicle.Id && !a.HasConflict))
                        .ToList();

                    double totalDistance = vehicleTrips.Sum(t => t.DistanceKm ?? 0);
                    double totalFuelNeeded = vehicle.CalculateFuelConsumption(totalDistance);

                    vehicleFuelTrackers[vehicle.Id] = new VehicleFuelTracker
                    {
                        Vehicle = vehicle,
                        TotalDistanceKm = totalDistance,
                        TotalFuelNeeded = totalFuelNeeded,
                        CurrentFuelLevel = vehicle.InitialFuelLevel, // Start with initial level
                        TripsByDate = vehicleTrips
                            .GroupBy(t => t.PickUpDate.Date)
                            .ToDictionary(
                                g => g.Key,
                                g => g.OrderBy(t => t.GarageOutTime).ToList()
                            )
                    };

                    Console.WriteLine($"  Vehicle {vehicle.PlateNumber}: {totalDistance:F1}km planned, " +
                                      $"needs {totalFuelNeeded:F1}L ({vehicle.FuelType}), " +
                                      $"tank: {vehicle.FuelTankCapacity}L, initial: {vehicle.InitialFuelLevel:F1}L");
                }

                // 4. Collect statistics
                result.TotalFuelPurchased = gasPurchases.Sum(p => p.LitersAmount);
                result.TotalFuelConsumed = vehicleFuelTrackers.Values.Sum(v => v.TotalFuelNeeded);
                result.TotalDistanceDriven = vehicleFuelTrackers.Values.Sum(v => v.TotalDistanceKm);
                result.TotalCostUzs = gasPurchases.Sum(p => p.AmountUzs);

                Console.WriteLine($"\n--- PERIOD SUMMARY ---");
                Console.WriteLine($"Total distance: {result.TotalDistanceDriven:F1}km");
                Console.WriteLine($"Total fuel needed: {result.TotalFuelConsumed:F1}L");
                Console.WriteLine($"Total fuel purchased: {result.TotalFuelPurchased:F1}L");

                // Fuel type breakdown
                PrintFuelTypeBreakdown(gasPurchases, vehicleFuelTrackers);

                // 5. Run the DAILY allocation algorithm
                var newAllocations = AllocateFuelDaily(
                    gasPurchases.ToList(),
                    vehicleFuelTrackers,
                    periodId,
                    result
                );

                // 6. Save allocations
                if (newAllocations.Any())
                {
                    await _uow.FuelAllocations.AddRangeAsync(newAllocations);
                    await _uow.CompleteAsync();
                }

                // 7. Build final status for each vehicle
                foreach (var tracker in vehicleFuelTrackers.Values)
                {
                    var allocations = newAllocations.Where(a => a.VehicleId == tracker.Vehicle.Id).ToList();
                    var status = BuildVehicleFuelStatus(tracker, allocations, periodId);
                    result.VehicleStatuses.Add(status);
                }

                result.TotalFuelAllocated = newAllocations.Sum(a => a.LitersAllocated);
                result.UnallocatedFuel = result.TotalFuelPurchased - result.TotalFuelAllocated;
                result.VehiclesOk = result.VehicleStatuses.Count(s => s.Status == "OK");
                result.VehiclesWithIssues = result.VehicleStatuses.Count(s => s.Status != "OK");

                if (result.UnallocatedFuel > ALLOCATION_TOLERANCE)
                {
                    result.Warnings.Add($"{result.UnallocatedFuel:F1}L of fuel remains unallocated");
                }

                Console.WriteLine($"\n{'=',-60}");
                Console.WriteLine($"=== ALLOCATION COMPLETE ===");
                Console.WriteLine($"Allocated: {result.TotalFuelAllocated:F1}L to {result.VehicleStatuses.Count} vehicles");
                Console.WriteLine($"Unallocated: {result.UnallocatedFuel:F1}L");
                Console.WriteLine($"Vehicles OK: {result.VehiclesOk}, With Issues: {result.VehiclesWithIssues}");
                Console.WriteLine($"{'=',-60}\n");

                return new Response<FuelCalculationResultDto>(HttpStatusCode.OK, "Fuel allocation completed", result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fuel allocation error: {ex.Message}");
                return new Response<FuelCalculationResultDto>(HttpStatusCode.InternalServerError,
                    new List<string> { ex.Message, ex.StackTrace ?? "" });
            }
        }

    /// <summary>
    /// Threshold for allowing purchase splitting across multiple vehicles.
    /// Purchases larger than this can be split; smaller ones go to one vehicle only.
    /// </summary>
    private const double LARGE_PURCHASE_THRESHOLD = 29.9;
    
        /// <summary>
    /// NEW: Daily fuel allocation algorithm
    /// Processes day-by-day, deducting fuel for trips, then allocating purchases made that day
    /// </summary>
        /// <summary>
    /// NEW: Daily fuel allocation algorithm
    /// Processes day-by-day, deducting fuel for trips, then allocating purchases made that day
    /// 
    /// Rules:
    /// - Purchases ≤ 55L → ONE vehicle only (no splitting)
    /// - Purchases > 55L → CAN be split across multiple vehicles if needed
    /// - No overfueling (cannot exceed tank capacity)
    /// - АИ-92 and АИ-95 are interchangeable
    /// </summary>
    private List<VehicleFuelAllocation> AllocateFuelDaily(
        List<GasPurchase> purchases,
        Dictionary<int, VehicleFuelTracker> vehicleTrackers,
        int periodId,
        FuelCalculationResultDto result)
    {
        var allocations = new List<VehicleFuelAllocation>();

        // Get all unique dates from trips AND purchases
        var tripDates = vehicleTrackers.Values
            .SelectMany(v => v.TripsByDate.Keys)
            .ToHashSet();

        var purchaseDates = purchases
            .Select(p => p.PurchaseDate.Date)
            .ToHashSet();

        var allDates = tripDates.Union(purchaseDates).OrderBy(d => d).ToList();

        if (!allDates.Any())
        {
            result.Warnings.Add("No trips or purchases found in this period");
            return allocations;
        }

        Console.WriteLine($"\n=== DAILY ALLOCATION ===");
        Console.WriteLine($"Processing {allDates.Count} days from {allDates.First():yyyy-MM-dd} to {allDates.Last():yyyy-MM-dd}");
        Console.WriteLine($"Large purchase threshold (splittable): > {LARGE_PURCHASE_THRESHOLD}L");

        // Track which purchases have been used
        var availablePurchases = purchases
            .Where(p => p.AllocatedLiters < p.LitersAmount)
            .ToList();

        foreach (var date in allDates)
        {
            Console.WriteLine($"\n--- {date:yyyy-MM-dd} ---");

            // STEP 1: Process all trips for this day - DEDUCT fuel from tanks
            foreach (var tracker in vehicleTrackers.Values)
            {
                if (tracker.TripsByDate.TryGetValue(date, out var tripsToday))
                {
                    foreach (var trip in tripsToday)
                    {
                        double distanceKm = trip.DistanceKm ?? 0;
                        double fuelUsed = tracker.Vehicle.CalculateFuelConsumption(distanceKm);

                        tracker.CurrentFuelLevel -= fuelUsed;
                        tracker.FuelConsumedSoFar += fuelUsed;

                        Console.WriteLine($"  🚗 {tracker.Vehicle.PlateNumber}: Trip {distanceKm:F1}km, " +
                                          $"used {fuelUsed:F1}L, tank now: {tracker.CurrentFuelLevel:F1}L");
                    }
                }
            }

            // STEP 2: Get purchases made on THIS day
            var purchasesToday = availablePurchases
                .Where(p => p.PurchaseDate.Date == date && p.AllocatedLiters < p.LitersAmount)
                .OrderByDescending(p => p.LitersAmount) // Larger purchases first
                .ToList();

            if (!purchasesToday.Any())
            {
                continue;
            }

            Console.WriteLine($"  ⛽ {purchasesToday.Count} purchase(s) available today");

            // STEP 3: Allocate each purchase
            foreach (var purchase in purchasesToday)
            {
                double remainingInPurchase = purchase.LitersAmount - purchase.AllocatedLiters;

                if (remainingInPurchase < ALLOCATION_TOLERANCE)
                    continue;

                bool isLargePurchase = purchase.LitersAmount > LARGE_PURCHASE_THRESHOLD;
                bool isFirstAllocationForThisPurchase = true;

                Console.WriteLine($"    Processing: {remainingInPurchase:F1}L {purchase.FuelType} " +
                                  $"(splittable: {(isLargePurchase ? "YES" : "NO")})");

                // Loop to potentially allocate to multiple vehicles (only if large purchase)
                while (remainingInPurchase > ALLOCATION_TOLERANCE)
                {
                    // Find compatible vehicles that can accept fuel
                    var compatibleVehicles = vehicleTrackers.Values
                        .Where(v => AreFuelTypesCompatible(v.Vehicle.FuelType, purchase.FuelType))
                        .Where(v => v.CurrentFuelLevel < v.Vehicle.FuelTankCapacity) // Has room in tank
                        .OrderBy(v => v.CurrentFuelLevel) // Lowest fuel level first (most urgent)
                        .ThenByDescending(v => v.TotalFuelNeeded) // Tie-breaker: vehicles that need more overall
                        .ToList();

                    if (!compatibleVehicles.Any())
                    {
                        if (isFirstAllocationForThisPurchase)
                        {
                            // Check if it's a fuel type mismatch issue
                            var anyCompatible = vehicleTrackers.Values
                                .Any(v => AreFuelTypesCompatible(v.Vehicle.FuelType, purchase.FuelType));

                            if (!anyCompatible)
                            {
                                result.Warnings.Add($"No vehicles configured for fuel type {purchase.FuelType}");
                            }
                            else
                            {
                                result.Warnings.Add($"All compatible vehicles have full tanks on {date:yyyy-MM-dd} " +
                                                    $"(purchase of {remainingInPurchase:F1}L {purchase.FuelType})");
                            }
                        }
                        else
                        {
                            // Already allocated some, just warn about remainder
                            result.Warnings.Add($"{remainingInPurchase:F1}L remaining from {date:yyyy-MM-dd} " +
                                                $"{purchase.FuelType} purchase could not be allocated (all tanks full)");
                        }
                        break; // Exit while loop, move to next purchase
                    }

                    // Select target vehicle
                    var targetVehicle = compatibleVehicles.First();
                    double tankCapacity = targetVehicle.Vehicle.FuelTankCapacity;
                    double currentLevel = targetVehicle.CurrentFuelLevel;

                    // Can only fill up to tank capacity - NO OVERFUELING
                    double maxCanAccept = Math.Max(0, tankCapacity - currentLevel);
                    double toAllocate = Math.Min(maxCanAccept, remainingInPurchase);

                    if (toAllocate < ALLOCATION_TOLERANCE)
                    {
                        Console.WriteLine($"      ⚠️ {targetVehicle.Vehicle.PlateNumber} tank nearly full, skipping");
                        // Remove this vehicle from consideration by marking it full temporarily
                        // (it will be filtered out in next iteration due to CurrentFuelLevel check)
                        break;
                    }

                    // Create allocation
                    var allocation = new VehicleFuelAllocation
                    {
                        GasPurchaseId = purchase.Id,
                        VehicleId = targetVehicle.Vehicle.Id,
                        ReportPeriodId = periodId,
                        LitersAllocated = Math.Round(toAllocate, 2),
                        AllocationCostUzs = purchase.LitersAmount > 0
                            ? Math.Round(purchase.AmountUzs * (decimal)(toAllocate / purchase.LitersAmount), 2)
                            : 0,
                        AllocationDate = date,
                        Reason = FuelAllocationReason.AutoDistanceBased,
                        Notes = isLargePurchase && !isFirstAllocationForThisPurchase
                            ? $"Split allocation: tank was {currentLevel:F1}L, filled to {currentLevel + toAllocate:F1}L"
                            : $"Daily allocation: tank was {currentLevel:F1}L, filled to {currentLevel + toAllocate:F1}L",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    allocations.Add(allocation);

                    // Update tracking
                    targetVehicle.CurrentFuelLevel += toAllocate;
                    targetVehicle.TotalFuelAllocated += toAllocate;
                    purchase.AllocatedLiters += toAllocate;
                    remainingInPurchase -= toAllocate;

                    Console.WriteLine($"      ✓ → {targetVehicle.Vehicle.PlateNumber}: +{toAllocate:F1}L " +
                                      $"(tank: {targetVehicle.CurrentFuelLevel:F1}/{tankCapacity}L)" +
                                      $"{(isLargePurchase ? $", remaining in purchase: {remainingInPurchase:F1}L" : "")}");

                    isFirstAllocationForThisPurchase = false;

                    // If NOT a large purchase, only allocate to ONE vehicle
                    if (!isLargePurchase)
                    {
                        if (remainingInPurchase > ALLOCATION_TOLERANCE)
                        {
                            result.Warnings.Add($"{remainingInPurchase:F1}L from {date:yyyy-MM-dd} {purchase.FuelType} " +
                                                $"purchase could not be allocated (small purchase, vehicle tank full)");
                        }
                        break; // Exit while loop, move to next purchase
                    }

                    // For large purchases, continue loop to try allocating remainder to other vehicles
                }
            }
        }

        // Final check - report vehicles with negative fuel (deficit)
        Console.WriteLine($"\n=== FINAL FUEL STATUS ===");
        foreach (var tracker in vehicleTrackers.Values)
        {
            string status = tracker.CurrentFuelLevel >= 0 ? "✓" : "⚠️ DEFICIT";
            Console.WriteLine($"  {status} {tracker.Vehicle.PlateNumber}: {tracker.CurrentFuelLevel:F1}L " +
                              $"(consumed: {tracker.FuelConsumedSoFar:F1}L, allocated: {tracker.TotalFuelAllocated:F1}L)");

            if (tracker.CurrentFuelLevel < -ALLOCATION_TOLERANCE)
            {
                result.Warnings.Add($"{tracker.Vehicle.PlateNumber}: Fuel deficit of {Math.Abs(tracker.CurrentFuelLevel):F1}L " +
                                    $"(insufficient fuel purchases to cover {tracker.FuelConsumedSoFar:F1}L consumed)");
            }
        }

        return allocations;
    }


    /// <summary>
    /// Check if fuel types are compatible:
    /// - Gasoline types (АИ-92, АИ-95) are interchangeable
    /// - Diesel only matches Diesel
    /// </summary>
    private bool AreFuelTypesCompatible(string vehicleFuelType, string purchaseFuelType)
    {
        if (string.IsNullOrEmpty(vehicleFuelType) || string.IsNullOrEmpty(purchaseFuelType))
            return false;

        var vType = vehicleFuelType.ToLower().Trim();
        var pType = purchaseFuelType.ToLower().Trim();

        // Exact match always works
        if (vType == pType)
            return true;

        // Diesel types - must match diesel
        var dieselTypes = new[] { "дт", "diesel", "дизель", "дизельное", "dt" };
        bool isVehicleDiesel = dieselTypes.Any(d => vType.Contains(d));
        bool isPurchaseDiesel = dieselTypes.Any(d => pType.Contains(d));

        if (isVehicleDiesel || isPurchaseDiesel)
        {
            // If either is diesel, both must be diesel
            return isVehicleDiesel && isPurchaseDiesel;
        }

        // Gasoline types - АИ-92, АИ-95, АИ-80 are all interchangeable
        var gasolineTypes = new[] { "аи-92", "аи-95", "аи-80", "аи92", "аи95", "аи80",
            "92", "95", "80", "бензин", "gasoline", "petrol" };
        bool isVehicleGasoline = gasolineTypes.Any(g => vType.Contains(g));
        bool isPurchaseGasoline = gasolineTypes.Any(g => pType.Contains(g));

        return isVehicleGasoline && isPurchaseGasoline;
    }

    /// <summary>
    /// Build status for a vehicle based on tracker and allocations
    /// </summary>
    private VehicleFuelStatusDto BuildVehicleFuelStatus(
        VehicleFuelTracker tracker,
        List<VehicleFuelAllocation> allocations,
        int periodId)
    {
        var vehicle = tracker.Vehicle;

        // Final fuel level already calculated through daily simulation
        double currentFuelLevel = tracker.CurrentFuelLevel;

        // Determine status
        string status;
        var warnings = new List<string>();

        if (vehicle.FuelTankCapacity <= 0 || vehicle.FuelConsumptionPer100Km <= 0)
        {
            status = "NOT_CONFIGURED";
            warnings.Add("Vehicle fuel configuration incomplete");
        }
        else if (currentFuelLevel < -ALLOCATION_TOLERANCE)
        {
            status = "NEGATIVE";
            warnings.Add($"Fuel deficit of {Math.Abs(currentFuelLevel):F1}L - insufficient fuel purchases");
        }
        else if (currentFuelLevel < vehicle.FuelTankCapacity * 0.1) // Less than 10% of tank
        {
            status = "LOW";
            warnings.Add($"Low fuel level: {currentFuelLevel:F1}L");
        }
        else
        {
            status = "OK";
        }

        // Calculate total cost from allocations
        decimal totalCost = allocations.Sum(a => a.AllocationCostUzs);

        return new VehicleFuelStatusDto
        {
            VehicleId = vehicle.Id,
            PlateNumber = vehicle.PlateNumber,
            Model = vehicle.Model,
            FuelType = vehicle.FuelType,
            TankCapacity = vehicle.FuelTankCapacity,
            ConsumptionPer100Km = vehicle.FuelConsumptionPer100Km,

            // Period-specific data
            InitialFuelLevel = vehicle.InitialFuelLevel,
            TotalDistanceDriven = tracker.TotalDistanceKm,
            FuelConsumed = tracker.FuelConsumedSoFar,
            FuelAllocated = tracker.TotalFuelAllocated,
            CurrentFuelLevel = currentFuelLevel,
            TotalFuelCostUzs = totalCost,

            // Status
            Status = status,
            Warnings = warnings,

            // Detailed allocations
            Allocations = allocations.Select(a => new FuelAllocationDetailDto
            {
                Id = a.Id,
                AllocationDate = a.AllocationDate,
                LitersAllocated = a.LitersAllocated,
                CostUzs = a.AllocationCostUzs,
                FuelType = a.GasPurchase?.FuelType ?? vehicle.FuelType,
                Reason = a.Reason.ToString(),
                TripConfNumber = null, // Could be enhanced to link to specific trip
                Notes = a.Notes
            }).ToList()
        };
    }


    private void PrintFuelTypeBreakdown(
        IEnumerable<GasPurchase> purchases,
        Dictionary<int, VehicleFuelTracker> trackers)
    {
        var purchasesByType = purchases.GroupBy(p => NormalizeFuelType(p.FuelType));

        Console.WriteLine($"\n=== FUEL TYPE BREAKDOWN ===");

        foreach (var group in purchasesByType)
        {
            var fuelType = group.Key;
            var purchasedAmount = group.Sum(p => p.LitersAmount);

            // Find compatible vehicles
            var compatibleVehicles = trackers.Values
                .Where(v => AreFuelTypesCompatible(v.Vehicle.FuelType, group.First().FuelType))
                .ToList();

            var neededAmount = compatibleVehicles.Sum(v => v.TotalFuelNeeded);
            var vehicleCount = compatibleVehicles.Count;

            Console.WriteLine($"  {fuelType}:");
            Console.WriteLine($"    Purchased: {purchasedAmount:F1}L");
            Console.WriteLine($"    Needed: {neededAmount:F1}L ({vehicleCount} vehicles)");
            Console.WriteLine($"    Balance: {purchasedAmount - neededAmount:F1}L");
        }
        Console.WriteLine($"=============================\n");
    }

    private string NormalizeFuelType(string fuelType)
    {
        var lower = fuelType?.ToLower().Trim() ?? "";

        if (lower.Contains("дт") || lower.Contains("diesel") || lower.Contains("дизель"))
            return "Diesel/ДТ";

        if (lower.Contains("92") || lower.Contains("95") || lower.Contains("80") ||
            lower.Contains("бензин") || lower.Contains("gasoline"))
            return "Gasoline/АИ";

        return fuelType ?? "Unknown";
    }

    /// <summary>
    /// Tracking class for daily fuel simulation
    /// </summary>
    private class VehicleFuelTracker
    {
        public Domain.Entities.Vehicle Vehicle { get; set; } = null!;
        public double TotalDistanceKm { get; set; }
        public double TotalFuelNeeded { get; set; }
        public double CurrentFuelLevel { get; set; }      // Changes daily as we simulate
        public double FuelConsumedSoFar { get; set; }     // Running total of fuel used
        public double TotalFuelAllocated { get; set; }    // Running total of fuel received
        public Dictionary<DateTime, List<Domain.Entities.Trip>> TripsByDate { get; set; } = new();
    }


    public async Task<Response<FuelCalculationResultDto>> PreviewFuelAllocationAsync(int periodId)
    {
        try
        {
            Console.WriteLine($"\n--- PREVIEW MODE (no changes will be saved) ---\n");

            var result = new FuelCalculationResultDto
            {
                ReportPeriodId = periodId,
                CalculatedAt = DateTime.UtcNow,
                Success = true
            };

            var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
            if (period == null)
            {
                result.Success = false;
                result.Errors.Add("Report period not found");
                return new Response<FuelCalculationResultDto>(HttpStatusCode.NotFound, result);
            }

            var vehicles = await _uow.Vehicles.GetAllAsync();
            var gasPurchases = await _uow.GasPurchases.GetByPeriodIdAsync(periodId);

            if (!gasPurchases.Any())
            {
                result.Success = false;
                result.Errors.Add("No gas purchases found for this period");
                return new Response<FuelCalculationResultDto>(HttpStatusCode.BadRequest, result);
            }

            foreach (var vehicle in vehicles.Where(v => 
                         !string.IsNullOrEmpty(v.FuelType) && 
                         !v.FuelType.Equals("Electric", StringComparison.OrdinalIgnoreCase) &&
                         !v.FuelType.Equals("Электро", StringComparison.OrdinalIgnoreCase)))
            {
                var vehicleTrips = period.Trips
                    .Where(t => t.Assignments.Any(a => a.VehicleId == vehicle.Id && !a.HasConflict))
                    .ToList();

                double totalDistance = vehicleTrips.Sum(t => t.DistanceKm ?? 0);
                double fuelNeeded = vehicle.CalculateFuelConsumption(totalDistance);
                double endingLevel = vehicle.InitialFuelLevel - fuelNeeded;

                var sameTypePurchases = gasPurchases.Where(p => p.FuelType.ToLower() == vehicle.FuelType.ToLower()).Sum(p => p.LitersAmount);
                var sameTypeVehicles = vehicles.Count(v => v.FuelType.ToLower() == vehicle.FuelType.ToLower());
                double estimatedAllocation = sameTypeVehicles > 0 ? sameTypePurchases / sameTypeVehicles : 0;

                var status = new VehicleFuelStatusDto
                {
                    VehicleId = vehicle.Id,
                    PlateNumber = vehicle.PlateNumber,
                    Model = vehicle.Model,
                    FuelType = vehicle.FuelType,
                    TankCapacity = vehicle.FuelTankCapacity,
                    ConsumptionPer100Km = vehicle.FuelConsumptionPer100Km,
                    InitialFuelLevel = vehicle.InitialFuelLevel,
                    TotalDistanceDriven = totalDistance,
                    FuelConsumed = fuelNeeded,
                    FuelAllocated = 0,
                    CurrentFuelLevel = endingLevel
                };

                if (fuelNeeded > vehicle.InitialFuelLevel && estimatedAllocation < fuelNeeded)
                {
                    status.Status = "NEEDS_FUEL";
                    status.Warnings.Add($"Needs approximately {fuelNeeded:F1}L");
                }
                else
                {
                    status.Status = "OK";
                }

                result.VehicleStatuses.Add(status);
            }

            result.TotalFuelPurchased = gasPurchases.Sum(p => p.LitersAmount);
            result.TotalFuelConsumed = result.VehicleStatuses.Sum(s => s.FuelConsumed);
            result.TotalDistanceDriven = result.VehicleStatuses.Sum(s => s.TotalDistanceDriven);
            result.TotalCostUzs = gasPurchases.Sum(p => p.AmountUzs);
            result.Messages.Add("PREVIEW MODE - No changes saved. Run allocation to apply.");

            return new Response<FuelCalculationResultDto>(HttpStatusCode.OK, "Preview completed", result);
        }
        catch (Exception ex)
        {
            return new Response<FuelCalculationResultDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<string>> ManualFuelAllocationAsync(ManualFuelAllocationRequest request)
    {
        try
        {
            var purchase = await _uow.GasPurchases.GetByIdAsync(request.GasPurchaseId);
            if (purchase == null)
                return new Response<string>(HttpStatusCode.NotFound, "Gas purchase not found");

            var vehicle = await _uow.Vehicles.GetByIdAsync(request.VehicleId);
            if (vehicle == null)
                return new Response<string>(HttpStatusCode.NotFound, "Vehicle not found");

            if (!vehicle.IsCompatibleFuelType(purchase.FuelType))
            {
                return new Response<string>(HttpStatusCode.BadRequest,
                    $"Fuel type mismatch: Vehicle uses {vehicle.FuelType}, purchase is {purchase.FuelType}");
            }

            if (request.LitersToAllocate > purchase.RemainingLiters + ALLOCATION_TOLERANCE)
            {
                return new Response<string>(HttpStatusCode.BadRequest,
                    $"Insufficient fuel available. Remaining: {purchase.RemainingLiters:F1}L, Requested: {request.LitersToAllocate:F1}L");
            }

            var allocation = new VehicleFuelAllocation
            {
                GasPurchaseId = purchase.Id,
                VehicleId = vehicle.Id,
                ReportPeriodId = purchase.ReportPeriodId,
                LitersAllocated = request.LitersToAllocate,
                AllocationCostUzs = purchase.LitersAmount > 0
                    ? Math.Round(purchase.AmountUzs * (decimal)(request.LitersToAllocate / purchase.LitersAmount), 2)
                    : 0,
                AllocationDate = purchase.PurchaseDate,
                Reason = FuelAllocationReason.ManualAllocation,
                Notes = request.Notes ?? "Manual allocation",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            purchase.AllocatedLiters += request.LitersToAllocate;

            await _uow.FuelAllocations.AddAsync(allocation);
            await _uow.CompleteAsync();

            return new Response<string>(HttpStatusCode.OK,
                $"Allocated {request.LitersToAllocate:F1}L to {vehicle.PlateNumber}", "Success");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<List<VehicleFuelStatusDto>>> GetVehicleFuelStatusAsync(int periodId)
    {
        try
        {
            var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
            if (period == null)
                return new Response<List<VehicleFuelStatusDto>>(HttpStatusCode.NotFound, "Period not found");

            var vehicles = await _uow.Vehicles.GetAllAsync();
            var allocations = await _uow.FuelAllocations.GetByPeriodIdAsync(periodId);

            var statuses = new List<VehicleFuelStatusDto>();

            foreach (var vehicle in vehicles.Where(v => 
                         !string.IsNullOrEmpty(v.FuelType) && 
                         !v.FuelType.Equals("Electric", StringComparison.OrdinalIgnoreCase) &&
                         !v.FuelType.Equals("Электро", StringComparison.OrdinalIgnoreCase)))
            {
                var vehicleTrips = period.Trips
                    .Where(t => t.Assignments.Any(a => a.VehicleId == vehicle.Id && !a.HasConflict))
                    .ToList();

                double totalDistance = vehicleTrips.Sum(t => t.DistanceKm ?? 0);
                double fuelNeeded = vehicle.CalculateFuelConsumption(totalDistance);

                var vehicleAllocations = allocations.Where(a => a.VehicleId == vehicle.Id).ToList();
                double totalAllocated = vehicleAllocations.Sum(a => a.LitersAllocated);
                decimal totalCost = vehicleAllocations.Sum(a => a.AllocationCostUzs);
                double endingLevel = vehicle.InitialFuelLevel + totalAllocated - fuelNeeded;

                var status = new VehicleFuelStatusDto
                {
                    VehicleId = vehicle.Id,
                    PlateNumber = vehicle.PlateNumber,
                    Model = vehicle.Model,
                    FuelType = vehicle.FuelType,
                    TankCapacity = vehicle.FuelTankCapacity,
                    ConsumptionPer100Km = vehicle.FuelConsumptionPer100Km,
                    InitialFuelLevel = vehicle.InitialFuelLevel,
                    TotalDistanceDriven = totalDistance,
                    FuelConsumed = fuelNeeded,
                    FuelAllocated = totalAllocated,
                    CurrentFuelLevel = endingLevel,
                    TotalFuelCostUzs = totalCost,
                    Allocations = vehicleAllocations.Select(a => new FuelAllocationDetailDto
                    {
                        Id = a.Id,
                        AllocationDate = a.AllocationDate,
                        LitersAllocated = a.LitersAllocated,
                        CostUzs = a.AllocationCostUzs,
                        FuelType = vehicle.FuelType,
                        Reason = a.Reason.ToString(),
                        Notes = a.Notes
                    }).ToList()
                };

                if (endingLevel < 0)
                {
                    status.Status = "NEGATIVE";
                    status.Warnings.Add($"Fuel level goes negative ({endingLevel:F1}L)");
                }
                else if (vehicle.FuelTankCapacity > 0 && endingLevel > vehicle.FuelTankCapacity)
                {
                    status.Status = "OVER_CAPACITY";
                    status.Warnings.Add($"Exceeds tank capacity ({endingLevel:F1}L > {vehicle.FuelTankCapacity}L)");
                }
                else if (endingLevel < MIN_FUEL_RESERVE)
                {
                    status.Status = "LOW";
                    status.Warnings.Add($"Low fuel: {endingLevel:F1}L");
                }
                else
                {
                    status.Status = "OK";
                }

                statuses.Add(status);
            }

            return new Response<List<VehicleFuelStatusDto>>(HttpStatusCode.OK, statuses);
        }
        catch (Exception ex)
        {
            return new Response<List<VehicleFuelStatusDto>>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<FuelBalancePreviewDto>> GetVehicleFuelBalanceAsync(int vehicleId, int periodId)
    {
        try
        {
            var vehicle = await _uow.Vehicles.GetByIdAsync(vehicleId);
            if (vehicle == null)
                return new Response<FuelBalancePreviewDto>(HttpStatusCode.NotFound, "Vehicle not found");

            var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
            if (period == null)
                return new Response<FuelBalancePreviewDto>(HttpStatusCode.NotFound, "Period not found");

            var allocations = await _uow.FuelAllocations.GetByVehicleAndPeriodAsync(vehicleId, periodId);

            var vehicleTrips = period.Trips
                .Where(t => t.Assignments.Any(a => a.VehicleId == vehicleId && !a.HasConflict))
                .OrderBy(t => t.PickUpDate)
                .ThenBy(t => t.GarageOutTime)
                .ToList();

            var dailyBalances = new List<DailyFuelBalanceDto>();
            double runningBalance = vehicle.InitialFuelLevel;

            var allDates = new HashSet<DateTime>();
            allDates.UnionWith(vehicleTrips.Select(t => t.PickUpDate.Date));
            allDates.UnionWith(allocations.Select(a => a.AllocationDate.Date));

            foreach (var date in allDates.OrderBy(d => d))
            {
                var dayTrips = vehicleTrips.Where(t => t.PickUpDate.Date == date).ToList();
                var dayAllocations = allocations.Where(a => a.AllocationDate.Date == date).ToList();

                double distanceToday = dayTrips.Sum(t => t.DistanceKm ?? 0);
                double fuelUsed = vehicle.CalculateFuelConsumption(distanceToday);
                double fuelAdded = dayAllocations.Sum(a => a.LitersAllocated);

                double startingBalance = runningBalance;
                double endingBalance = startingBalance + fuelAdded - fuelUsed;

                var dailyBalance = new DailyFuelBalanceDto
                {
                    Date = date,
                    StartingBalance = Math.Round(startingBalance, 2),
                    FuelUsed = Math.Round(fuelUsed, 2),
                    FuelAdded = Math.Round(fuelAdded, 2),
                    EndingBalance = Math.Round(endingBalance, 2),
                    DistanceDriven = Math.Round(distanceToday, 1),
                    TripConfNumbers = dayTrips.Select(t => t.ConfNumber ?? "").ToList()
                };

                if (endingBalance < 0)
                {
                    dailyBalance.HasWarning = true;
                    dailyBalance.WarningMessage = $"Negative balance: {endingBalance:F1}L";
                }
                else if (endingBalance < MIN_FUEL_RESERVE)
                {
                    dailyBalance.HasWarning = true;
                    dailyBalance.WarningMessage = $"Low fuel warning: {endingBalance:F1}L";
                }

                dailyBalances.Add(dailyBalance);
                runningBalance = endingBalance;
            }

            var result = new FuelBalancePreviewDto
            {
                VehicleId = vehicleId,
                PlateNumber = vehicle.PlateNumber,
                DailyBalances = dailyBalances
            };

            return new Response<FuelBalancePreviewDto>(HttpStatusCode.OK, result);
        }
        catch (Exception ex)
        {
            return new Response<FuelBalancePreviewDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<FuelCalculationResultDto>> ValidateFuelAllocationsAsync(int periodId)
    {
        var statusResult = await GetVehicleFuelStatusAsync(periodId);

        if (statusResult.StatusCode != 200)
        {
            return new Response<FuelCalculationResultDto>((HttpStatusCode)statusResult.StatusCode, statusResult.Message);
        }

        var result = new FuelCalculationResultDto
        {
            ReportPeriodId = periodId,
            CalculatedAt = DateTime.UtcNow,
            VehicleStatuses = statusResult.Data ?? new List<VehicleFuelStatusDto>()
        };

        result.VehiclesOk = result.VehicleStatuses.Count(s => s.Status == "OK");
        result.VehiclesWithIssues = result.VehicleStatuses.Count(s => s.Status != "OK");
        result.Success = result.VehiclesWithIssues == 0;

        foreach (var status in result.VehicleStatuses.Where(s => s.Warnings.Any()))
        {
            foreach (var warning in status.Warnings)
            {
                result.Warnings.Add($"{status.PlateNumber}: {warning}");
            }
        }

        result.TotalDistanceDriven = result.VehicleStatuses.Sum(s => s.TotalDistanceDriven);
        result.TotalFuelConsumed = result.VehicleStatuses.Sum(s => s.FuelConsumed);
        result.TotalFuelAllocated = result.VehicleStatuses.Sum(s => s.FuelAllocated);
        result.TotalCostUzs = result.VehicleStatuses.Sum(s => s.TotalFuelCostUzs);

        return new Response<FuelCalculationResultDto>(
            HttpStatusCode.OK,
            result.Success ? "Validation passed" : $"Validation found {result.VehiclesWithIssues} vehicles with issues",
            result
        );
    }
    
    /// <summary>
    /// Finalize fuel allocation for a period.
    /// This confirms the allocation is correct and updates vehicle initial fuel levels
    /// for the next period based on their ending fuel levels.
    /// </summary>
    public async Task<Response<FuelFinalizationResultDto>> FinalizeFuelAllocationAsync(int periodId)
    {
        try
        {
            // 1. Get the period
            var period = await _uow.ReportPeriods.GetByIdAsync(periodId);
            if (period == null)
            {
                return new Response<FuelFinalizationResultDto>(
                    HttpStatusCode.NotFound, 
                    "Report period not found"
                );
            }

            // 2. Check if already finalized
            if (period.IsFuelFinalized)
            {
                return new Response<FuelFinalizationResultDto>(
                    HttpStatusCode.BadRequest,
                    $"Period already finalized on {period.FuelFinalizedAt:yyyy-MM-dd HH:mm}"
                );
            }

            // 3. Get allocations for this period
            var allocations = await _uow.FuelAllocations.GetByPeriodIdAsync(periodId);
            if (!allocations.Any())
            {
                return new Response<FuelFinalizationResultDto>(
                    HttpStatusCode.BadRequest,
                    "No fuel allocations found. Please run fuel allocation first."
                );
            }

            // 4. Get all vehicles and calculate their final fuel levels
            var vehicles = await _uow.Vehicles.GetAllAsync();
            var gasPurchases = await _uow.GasPurchases.GetByPeriodIdAsync(periodId);
            var periodWithTrips = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);

            var result = new FuelFinalizationResultDto
            {
                PeriodId = periodId,
                FinalizedAt = DateTime.UtcNow,
                VehicleUpdates = new List<VehicleFuelUpdateDto>()
            };

            // 5. Calculate final fuel level for each vehicle
            foreach (var vehicle in vehicles.Where(v => 
                !string.IsNullOrEmpty(v.FuelType) &&
                !v.FuelType.Equals("Electric", StringComparison.OrdinalIgnoreCase) &&
                !v.FuelType.Equals("Электро", StringComparison.OrdinalIgnoreCase)))
            {
                // Get trips for this vehicle in this period
                var vehicleTrips = periodWithTrips!.Trips
                    .Where(t => t.Assignments.Any(a => a.VehicleId == vehicle.Id && !a.HasConflict))
                    .ToList();

                // Calculate fuel consumed
                double totalDistance = vehicleTrips.Sum(t => t.DistanceKm ?? 0);
                double fuelConsumed = vehicle.CalculateFuelConsumption(totalDistance);

                // Get fuel allocated to this vehicle
                double fuelAllocated = allocations
                    .Where(a => a.VehicleId == vehicle.Id)
                    .Sum(a => a.LitersAllocated);

                // Calculate final fuel level
                // Final = Initial + Allocated - Consumed
                double previousInitial = vehicle.InitialFuelLevel;
                double finalFuelLevel = previousInitial + fuelAllocated - fuelConsumed;

                // Clamp to valid range (0 to tank capacity)
                // Note: We allow negative values to show deficit, but for next period's initial,
                // we should probably floor at 0 (you can't start with negative fuel)
                double newInitialForNextPeriod = Math.Max(0, Math.Min(finalFuelLevel, vehicle.FuelTankCapacity));

                // Track the update
                result.VehicleUpdates.Add(new VehicleFuelUpdateDto
                {
                    VehicleId = vehicle.Id,
                    PlateNumber = vehicle.PlateNumber,
                    FuelType = vehicle.FuelType,
                    PreviousInitialLevel = previousInitial,
                    FuelAllocated = fuelAllocated,
                    FuelConsumed = fuelConsumed,
                    CalculatedFinalLevel = finalFuelLevel,
                    NewInitialLevel = newInitialForNextPeriod,
                    HasDeficit = finalFuelLevel < 0
                });

                // Update vehicle's initial fuel level for next period
                vehicle.InitialFuelLevel = newInitialForNextPeriod;
                vehicle.UpdatedAt = DateTime.UtcNow;
            }

            // 6. Mark period as finalized
            period.IsFuelFinalized = true;
            period.FuelFinalizedAt = DateTime.UtcNow;
            period.UpdatedAt = DateTime.UtcNow;

            // 7. Save all changes
            await _uow.CompleteAsync();

            result.Success = true;
            result.Message = $"Fuel allocation finalized. {result.VehicleUpdates.Count} vehicles updated.";
            result.VehiclesWithDeficit = result.VehicleUpdates.Count(v => v.HasDeficit);

            Console.WriteLine($"\n{'=',-60}");
            Console.WriteLine($"=== FUEL ALLOCATION FINALIZED FOR PERIOD {periodId} ===");
            Console.WriteLine($"Vehicles updated: {result.VehicleUpdates.Count}");
            Console.WriteLine($"Vehicles with deficit: {result.VehiclesWithDeficit}");
            Console.WriteLine($"{'=',-60}\n");

            return new Response<FuelFinalizationResultDto>(HttpStatusCode.OK, result.Message, result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Finalization error: {ex.Message}");
            return new Response<FuelFinalizationResultDto>(
                HttpStatusCode.InternalServerError,
                ex.Message
            );
        }
    }

    /// <summary>
    /// Preview what finalization would do without actually saving changes.
    /// Useful for showing user the impact before they confirm.
    /// </summary>
    public async Task<Response<FuelFinalizationResultDto>> PreviewFinalizationAsync(int periodId)
    {
        try
        {
            var period = await _uow.ReportPeriods.GetByIdAsync(periodId);
            if (period == null)
            {
                return new Response<FuelFinalizationResultDto>(
                    HttpStatusCode.NotFound, 
                    "Report period not found"
                );
            }

            if (period.IsFuelFinalized)
            {
                return new Response<FuelFinalizationResultDto>(
                    HttpStatusCode.BadRequest,
                    $"Period already finalized on {period.FuelFinalizedAt:yyyy-MM-dd HH:mm}"
                );
            }

            var allocations = await _uow.FuelAllocations.GetByPeriodIdAsync(periodId);
            if (!allocations.Any())
            {
                return new Response<FuelFinalizationResultDto>(
                    HttpStatusCode.BadRequest,
                    "No fuel allocations found. Please run fuel allocation first."
                );
            }

            var vehicles = await _uow.Vehicles.GetAllAsync();
            var periodWithTrips = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);

            var result = new FuelFinalizationResultDto
            {
                PeriodId = periodId,
                FinalizedAt = DateTime.UtcNow,
                IsPreview = true,
                VehicleUpdates = new List<VehicleFuelUpdateDto>()
            };

            foreach (var vehicle in vehicles.Where(v => 
                !string.IsNullOrEmpty(v.FuelType) &&
                !v.FuelType.Equals("Electric", StringComparison.OrdinalIgnoreCase) &&
                !v.FuelType.Equals("Электро", StringComparison.OrdinalIgnoreCase)))
            {
                var vehicleTrips = periodWithTrips!.Trips
                    .Where(t => t.Assignments.Any(a => a.VehicleId == vehicle.Id && !a.HasConflict))
                    .ToList();

                double totalDistance = vehicleTrips.Sum(t => t.DistanceKm ?? 0);
                double fuelConsumed = vehicle.CalculateFuelConsumption(totalDistance);
                double fuelAllocated = allocations
                    .Where(a => a.VehicleId == vehicle.Id)
                    .Sum(a => a.LitersAllocated);

                double previousInitial = vehicle.InitialFuelLevel;
                double finalFuelLevel = previousInitial + fuelAllocated - fuelConsumed;
                double newInitialForNextPeriod = Math.Max(0, Math.Min(finalFuelLevel, vehicle.FuelTankCapacity));

                result.VehicleUpdates.Add(new VehicleFuelUpdateDto
                {
                    VehicleId = vehicle.Id,
                    PlateNumber = vehicle.PlateNumber,
                    FuelType = vehicle.FuelType,
                    PreviousInitialLevel = previousInitial,
                    FuelAllocated = fuelAllocated,
                    FuelConsumed = fuelConsumed,
                    CalculatedFinalLevel = finalFuelLevel,
                    NewInitialLevel = newInitialForNextPeriod,
                    HasDeficit = finalFuelLevel < 0
                });
            }

            result.Success = true;
            result.Message = "Preview generated. No changes have been saved.";
            result.VehiclesWithDeficit = result.VehicleUpdates.Count(v => v.HasDeficit);

            return new Response<FuelFinalizationResultDto>(HttpStatusCode.OK, result.Message, result);
        }
        catch (Exception ex)
        {
            return new Response<FuelFinalizationResultDto>(
                HttpStatusCode.InternalServerError,
                ex.Message
            );
        }
    }

    /// <summary>
    /// Revert a finalized period (admin/undo functionality).
    /// This restores vehicle initial levels to what they were before finalization.
    /// </summary>
    public async Task<Response<string>> RevertFinalizationAsync(int periodId)
    {
        try
        {
            var period = await _uow.ReportPeriods.GetByIdAsync(periodId);
            if (period == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, "Report period not found");
            }

            if (!period.IsFuelFinalized)
            {
                return new Response<string>(HttpStatusCode.BadRequest, "Period is not finalized");
            }

            // To properly revert, we'd need to store the previous initial levels
            // For now, just unlock the period (manual correction of initial levels may be needed)
            period.IsFuelFinalized = false;
            period.FuelFinalizedAt = null;
            period.UpdatedAt = DateTime.UtcNow;

            await _uow.CompleteAsync();

            return new Response<string>(
                HttpStatusCode.OK,
                "Period finalization reverted. Note: Vehicle initial fuel levels may need manual correction."
            );
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }


    public async Task<Response<List<VehicleFuelStatusDto>>> GetFuelCostBreakdownAsync(int periodId)
    {
        return await GetVehicleFuelStatusAsync(periodId);
    }

    #endregion
    
    public async Task<Response<FuelDiagnosticDto>> GetFuelDiagnosticsAsync(int periodId)
    {
        try
        {
            var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
            if (period == null)
                return new Response<FuelDiagnosticDto>(HttpStatusCode.NotFound, "Period not found");

            var vehicles = await _uow.Vehicles.GetAllAsync();
            var gasPurchases = await _uow.GasPurchases.GetByPeriodIdAsync(periodId);

            var diagnostic = new FuelDiagnosticDto
            {
                ReportPeriodId = periodId,
                PeriodStart = period.StartDate,
                PeriodEnd = period.EndDate
            };

            // Group vehicles by fuel type and calculate consumption
            var vehiclesByFuelType = vehicles
                .Where(v => !string.IsNullOrEmpty(v.FuelType) &&
                           !v.FuelType.Equals("Electric", StringComparison.OrdinalIgnoreCase) &&
                           !v.FuelType.Equals("Электро", StringComparison.OrdinalIgnoreCase))
                .GroupBy(v => v.FuelType);

            foreach (var group in vehiclesByFuelType)
            {
                var fuelType = group.Key;
                var vehiclesInGroup = group.ToList();

                double totalDistance = 0;
                double totalFuelNeeded = 0;
                int vehicleCount = vehiclesInGroup.Count;
                int vehiclesWithTrips = 0;

                foreach (var vehicle in vehiclesInGroup)
                {
                    var vehicleTrips = period.Trips
                        .Where(t => t.Assignments.Any(a => a.VehicleId == vehicle.Id && !a.HasConflict))
                        .ToList();

                    double distance = vehicleTrips.Sum(t => t.DistanceKm ?? 0);
                    double fuelNeeded = vehicle.CalculateFuelConsumption(distance);

                    totalDistance += distance;
                    totalFuelNeeded += fuelNeeded;

                    if (vehicleTrips.Any())
                        vehiclesWithTrips++;
                }

                diagnostic.VehicleConsumptionByFuelType.Add(new FuelTypeConsumptionDto
                {
                    FuelType = fuelType,
                    VehicleCount = vehicleCount,
                    VehiclesWithTrips = vehiclesWithTrips,
                    TotalDistanceKm = Math.Round(totalDistance, 1),
                    TotalFuelNeeded = Math.Round(totalFuelNeeded, 1)
                });
            }

            // Group purchases by fuel type
            var purchasesByFuelType = gasPurchases.GroupBy(p => p.FuelType);

            foreach (var group in purchasesByFuelType)
            {
                diagnostic.PurchasesByFuelType.Add(new FuelTypePurchaseDto
                {
                    FuelType = group.Key,
                    PurchaseCount = group.Count(),
                    TotalLiters = Math.Round(group.Sum(p => p.LitersAmount), 1),
                    TotalCostUzs = group.Sum(p => p.AmountUzs)
                });
            }

            // Calculate balance per fuel type
            foreach (var consumption in diagnostic.VehicleConsumptionByFuelType)
            {
                var purchase = diagnostic.PurchasesByFuelType
                    .FirstOrDefault(p => p.FuelType.ToLower() == consumption.FuelType.ToLower());

                double purchased = purchase?.TotalLiters ?? 0;
                double needed = consumption.TotalFuelNeeded;
                double balance = purchased - needed;

                diagnostic.BalanceByFuelType.Add(new FuelTypeBalanceDto
                {
                    FuelType = consumption.FuelType,
                    TotalPurchased = purchased,
                    TotalNeeded = needed,
                    Balance = Math.Round(balance, 1),
                    Status = balance >= 0 ? "SURPLUS" : "DEFICIT"
                });
            }

            // Check for purchased fuel types with no vehicles
            foreach (var purchase in diagnostic.PurchasesByFuelType)
            {
                if (!diagnostic.VehicleConsumptionByFuelType.Any(v => v.FuelType == purchase.FuelType))
                {
                    diagnostic.BalanceByFuelType.Add(new FuelTypeBalanceDto
                    {
                        FuelType = purchase.FuelType,
                        TotalPurchased = purchase.TotalLiters,
                        TotalNeeded = 0,
                        Balance = purchase.TotalLiters,
                        Status = "NO_VEHICLES"
                    });
                }
            }

            // Summary
            diagnostic.TotalDistanceDriven = diagnostic.VehicleConsumptionByFuelType.Sum(v => v.TotalDistanceKm);
            diagnostic.TotalFuelNeeded = diagnostic.VehicleConsumptionByFuelType.Sum(v => v.TotalFuelNeeded);
            diagnostic.TotalFuelPurchased = diagnostic.PurchasesByFuelType.Sum(p => p.TotalLiters);
            diagnostic.OverallBalance = diagnostic.TotalFuelPurchased - diagnostic.TotalFuelNeeded;

            return new Response<FuelDiagnosticDto>(HttpStatusCode.OK, "Diagnostics generated", diagnostic);
        }
        catch (Exception ex)
        {
            return new Response<FuelDiagnosticDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    #region Export

    public async Task<byte[]> ExportFuelReportAsync(int periodId)
    {
        var statusResult = await GetVehicleFuelStatusAsync(periodId);
        var purchasesResult = await GetGasPurchasesAsync(periodId);

        using var workbook = new XLWorkbook();

        // Sheet 1: Vehicle Fuel Summary
        var summarySheet = workbook.Worksheets.Add("Vehicle Summary");
        var summaryHeaders = new[]
        {
            "Plate #", "Model", "Fuel Type", "Tank Capacity (L)", "Consumption (L/100km)",
            "Initial Level (L)", "Distance (km)", "Fuel Consumed (L)", "Fuel Allocated (L)",
            "Ending Level (L)", "Total Cost (UZS)", "Status"
        };

        for (int i = 0; i < summaryHeaders.Length; i++)
        {
            summarySheet.Cell(1, i + 1).Value = summaryHeaders[i];
            summarySheet.Cell(1, i + 1).Style.Font.Bold = true;
            summarySheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 2;
        foreach (var status in statusResult.Data ?? new List<VehicleFuelStatusDto>())
        {
            summarySheet.Cell(row, 1).Value = status.PlateNumber;
            summarySheet.Cell(row, 2).Value = status.Model ?? "";
            summarySheet.Cell(row, 3).Value = status.FuelType;
            summarySheet.Cell(row, 4).Value = status.TankCapacity;
            summarySheet.Cell(row, 5).Value = status.ConsumptionPer100Km;
            summarySheet.Cell(row, 6).Value = status.InitialFuelLevel;
            summarySheet.Cell(row, 7).Value = Math.Round(status.TotalDistanceDriven, 1);
            summarySheet.Cell(row, 8).Value = Math.Round(status.FuelConsumed, 2);
            summarySheet.Cell(row, 9).Value = Math.Round(status.FuelAllocated, 2);
            summarySheet.Cell(row, 10).Value = Math.Round(status.CurrentFuelLevel, 2);
            summarySheet.Cell(row, 11).Value = status.TotalFuelCostUzs;
            summarySheet.Cell(row, 12).Value = status.Status;

            switch (status.Status)
            {
                case "NEGATIVE":
                    summarySheet.Range(row, 1, row, 12).Style.Fill.BackgroundColor = XLColor.IndianRed;
                    break;
                case "OVER_CAPACITY":
                    summarySheet.Range(row, 1, row, 12).Style.Fill.BackgroundColor = XLColor.Orange;
                    break;
                case "LOW":
                    summarySheet.Range(row, 1, row, 12).Style.Fill.BackgroundColor = XLColor.Yellow;
                    break;
            }

            row++;
        }
        summarySheet.Columns().AdjustToContents();

        // Sheet 2: Gas Purchases
        var purchasesSheet = workbook.Worksheets.Add("Gas Purchases");
        var purchaseHeaders = new[]
        {
            "Date", "Liters", "Fuel Type", "Amount (UZS)", "Price/Liter",
            "Allocated (L)", "Remaining (L)", "Status"
        };

        for (int i = 0; i < purchaseHeaders.Length; i++)
        {
            purchasesSheet.Cell(1, i + 1).Value = purchaseHeaders[i];
            purchasesSheet.Cell(1, i + 1).Style.Font.Bold = true;
            purchasesSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        row = 2;
        foreach (var purchase in purchasesResult.Data ?? new List<GasPurchaseDto>())
        {
            purchasesSheet.Cell(row, 1).Value = purchase.PurchaseDate.ToString("yyyy-MM-dd");
            purchasesSheet.Cell(row, 2).Value = purchase.LitersAmount;
            purchasesSheet.Cell(row, 3).Value = purchase.FuelType;
            purchasesSheet.Cell(row, 4).Value = purchase.AmountUzs;
            purchasesSheet.Cell(row, 5).Value = purchase.PricePerLiter;
            purchasesSheet.Cell(row, 6).Value = Math.Round(purchase.AllocatedLiters, 2);
            purchasesSheet.Cell(row, 7).Value = Math.Round(purchase.RemainingLiters, 2);
            purchasesSheet.Cell(row, 8).Value = purchase.IsFullyAllocated ? "Allocated" : "Partial";

            if (!purchase.IsFullyAllocated)
            {
                purchasesSheet.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightYellow;
            }

            row++;
        }
        purchasesSheet.Columns().AdjustToContents();

        // Sheet 3: Summary by Fuel Type
        var byTypeSheet = workbook.Worksheets.Add("By Fuel Type");
        var summaryData = await GetGasPurchaseSummaryAsync(periodId);

        var typeHeaders = new[]
        {
            "Fuel Type", "Purchases", "Total Liters", "Total Amount (UZS)",
            "Avg Price/Liter", "Allocated (L)", "Remaining (L)"
        };

        for (int i = 0; i < typeHeaders.Length; i++)
        {
            byTypeSheet.Cell(1, i + 1).Value = typeHeaders[i];
            byTypeSheet.Cell(1, i + 1).Style.Font.Bold = true;
            byTypeSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        row = 2;
        foreach (var type in summaryData.Data?.ByFuelType ?? new List<FuelTypeSummaryDto>())
        {
            byTypeSheet.Cell(row, 1).Value = type.FuelType;
            byTypeSheet.Cell(row, 2).Value = type.PurchaseCount;
            byTypeSheet.Cell(row, 3).Value = type.TotalLiters;
            byTypeSheet.Cell(row, 4).Value = type.TotalAmountUzs;
            byTypeSheet.Cell(row, 5).Value = type.AveragePricePerLiter;
            byTypeSheet.Cell(row, 6).Value = Math.Round(type.AllocatedLiters, 2);
            byTypeSheet.Cell(row, 7).Value = Math.Round(type.RemainingLiters, 2);
            row++;
        }
        byTypeSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
    
    /// <summary>
    /// Export detailed fuel allocation breakdown to CSV files (zipped)
    /// </summary>
    public async Task<Response<FuelAllocationExportDto>> ExportFuelAllocationToCsvAsync(int periodId)
    {
        try
        {
            // 1. Get all required data
            var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
            if (period == null)
            {
                return new Response<FuelAllocationExportDto>(HttpStatusCode.NotFound, "Report period not found");
            }

            var vehicles = await _uow.Vehicles.GetAllAsync();
            var gasPurchases = await _uow.GasPurchases.GetByPeriodIdAsync(periodId);
            var allocations = await _uow.FuelAllocations.GetByPeriodIdAsync(periodId);

            // Filter to fuel-consuming vehicles only
            var fuelVehicles = vehicles.Where(v =>
                !string.IsNullOrEmpty(v.FuelType) &&
                !v.FuelType.Equals("Electric", StringComparison.OrdinalIgnoreCase) &&
                !v.FuelType.Equals("Электро", StringComparison.OrdinalIgnoreCase))
                .OrderBy(v => v.PlateNumber)
                .ToList();

            // 2. Generate CSV files and zip them
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // CSV 1: Summary
                var summaryCsv = GenerateSummaryCsv(period, fuelVehicles, gasPurchases.ToList(), allocations.ToList());
                AddCsvToZip(archive, "1_Summary.csv", summaryCsv);

                // CSV 2: Daily breakdown
                var dailyCsv = GenerateDailyBreakdownCsv(period, fuelVehicles, gasPurchases.ToList(), allocations.ToList());
                AddCsvToZip(archive, "2_DailyBreakdown.csv", dailyCsv);

                // CSV 3: All allocations
                var allocationsCsv = GenerateAllocationsCsv(allocations.ToList(), fuelVehicles, gasPurchases.ToList());
                AddCsvToZip(archive, "3_Allocations.csv", allocationsCsv);

                // CSV 4: Purchases
                var purchasesCsv = GeneratePurchasesCsv(gasPurchases.ToList());
                AddCsvToZip(archive, "4_Purchases.csv", purchasesCsv);
            }

            memoryStream.Position = 0;
            var fileName = $"FuelAllocation_Period{periodId}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

            return new Response<FuelAllocationExportDto>(HttpStatusCode.OK, new FuelAllocationExportDto
            {
                FileContent = memoryStream.ToArray(),
                FileName = fileName,
                ContentType = "application/zip"
            });
        }
        catch (Exception ex)
        {
            return new Response<FuelAllocationExportDto>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private void AddCsvToZip(ZipArchive archive, string fileName, string csvContent)
    {
        var entry = archive.CreateEntry(fileName);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(csvContent);
    }

    private string GenerateSummaryCsv(
        Domain.Entities.ReportPeriod period,
        List<Domain.Entities.Vehicle> vehicles,
        List<GasPurchase> purchases,
        List<VehicleFuelAllocation> allocations)
    {
        var sb = new StringBuilder();

        // Header info
        sb.AppendLine($"Fuel Allocation Report - Period {period.Id}");
        sb.AppendLine($"Period Start,{period.StartDate:yyyy-MM-dd}");
        sb.AppendLine($"Period End,{period.EndDate:yyyy-MM-dd}");
        sb.AppendLine($"Generated,{DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        // Overall statistics
        var totalPurchased = purchases.Sum(p => p.LitersAmount);
        var totalAllocated = allocations.Sum(a => a.LitersAllocated);
        var totalCost = purchases.Sum(p => p.AmountUzs);

        sb.AppendLine("OVERALL STATISTICS");
        sb.AppendLine($"Total Fuel Purchased (L),{totalPurchased:F2}");
        sb.AppendLine($"Total Fuel Allocated (L),{totalAllocated:F2}");
        sb.AppendLine($"Unallocated Fuel (L),{totalPurchased - totalAllocated:F2}");
        sb.AppendLine($"Total Cost (UZS),{totalCost:F0}");
        sb.AppendLine();

        // By fuel type
        sb.AppendLine("BY FUEL TYPE");
        sb.AppendLine("Fuel Type,Purchased (L),Allocated (L),Remaining (L),Vehicles");

        var purchasesByType = purchases.GroupBy(p => p.FuelType);
        foreach (var group in purchasesByType)
        {
            var purchased = group.Sum(p => p.LitersAmount);
            var allocated = group.Sum(p => p.AllocatedLiters);
            var vehicleCount = vehicles.Count(v => AreFuelTypesCompatible(v.FuelType, group.Key));
            sb.AppendLine($"{CsvEscape(group.Key)},{purchased:F2},{allocated:F2},{purchased - allocated:F2},{vehicleCount}");
        }
        sb.AppendLine();

        // Vehicle summary
        sb.AppendLine("VEHICLE SUMMARY");
        sb.AppendLine("Plate Number,Fuel Type,Tank Capacity,Initial Level,Distance (km),Fuel Consumed,Fuel Allocated,Final Level,Status");

        foreach (var vehicle in vehicles)
        {
            var vehicleTrips = period.Trips
                .Where(t => t.Assignments.Any(a => a.VehicleId == vehicle.Id && !a.HasConflict))
                .ToList();
            var distance = vehicleTrips.Sum(t => t.DistanceKm ?? 0);
            var consumed = vehicle.CalculateFuelConsumption(distance);
            var allocated = allocations.Where(a => a.VehicleId == vehicle.Id).Sum(a => a.LitersAllocated);
            var finalLevel = vehicle.InitialFuelLevel + allocated - consumed;

            string status = finalLevel < 0 ? "DEFICIT" : (finalLevel < vehicle.FuelTankCapacity * 0.1 ? "LOW" : "OK");

            sb.AppendLine($"{CsvEscape(vehicle.PlateNumber)},{CsvEscape(vehicle.FuelType)},{vehicle.FuelTankCapacity:F2},{vehicle.InitialFuelLevel:F2},{distance:F2},{consumed:F2},{allocated:F2},{finalLevel:F2},{status}");
        }

        return sb.ToString();
    }

    private string GenerateDailyBreakdownCsv(
        Domain.Entities.ReportPeriod period,
        List<Domain.Entities.Vehicle> vehicles,
        List<GasPurchase> purchases,
        List<VehicleFuelAllocation> allocations)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Vehicle,Fuel Type,Tank Capacity,Date,Start Level,Distance (km),Fuel Used,Fuel Added,End Level,Purchase Ref,Trip Refs");

        foreach (var vehicle in vehicles)
        {
            // Get trips and allocations for this vehicle
            var vehicleTrips = period.Trips
                .Where(t => t.Assignments.Any(a => a.VehicleId == vehicle.Id && !a.HasConflict))
                .ToList();

            var tripsByDate = vehicleTrips
                .GroupBy(t => t.PickUpDate.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            var vehicleAllocations = allocations
                .Where(a => a.VehicleId == vehicle.Id)
                .ToList();

            var allocationsByDate = vehicleAllocations
                .GroupBy(a => a.AllocationDate.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Get all dates
            var allDates = tripsByDate.Keys
                .Union(allocationsByDate.Keys)
                .OrderBy(d => d)
                .ToList();

            double currentLevel = vehicle.InitialFuelLevel;

            foreach (var date in allDates)
            {
                double startLevel = currentLevel;
                double distanceToday = 0;
                double fuelUsedToday = 0;
                double fuelAddedToday = 0;
                var purchaseRefs = new List<string>();
                var tripRefs = new List<string>();

                // Process trips
                if (tripsByDate.TryGetValue(date, out var tripsToday))
                {
                    foreach (var trip in tripsToday)
                    {
                        var dist = trip.DistanceKm ?? 0;
                        distanceToday += dist;
                        fuelUsedToday += vehicle.CalculateFuelConsumption(dist);
                        tripRefs.Add(trip.ConfNumber ?? "N/A");
                    }
                }

                currentLevel -= fuelUsedToday;

                // Process allocations
                if (allocationsByDate.TryGetValue(date, out var allocsToday))
                {
                    foreach (var alloc in allocsToday)
                    {
                        fuelAddedToday += alloc.LitersAllocated;
                        var purchase = purchases.FirstOrDefault(p => p.Id == alloc.GasPurchaseId);
                        purchaseRefs.Add($"#{alloc.GasPurchaseId}({purchase?.FuelType ?? "?"})");
                    }
                }

                currentLevel += fuelAddedToday;

                sb.AppendLine($"{CsvEscape(vehicle.PlateNumber)},{CsvEscape(vehicle.FuelType)},{vehicle.FuelTankCapacity:F2},{date:yyyy-MM-dd},{startLevel:F2},{distanceToday:F2},{fuelUsedToday:F2},{fuelAddedToday:F2},{currentLevel:F2},{CsvEscape(string.Join("; ", purchaseRefs))},{CsvEscape(string.Join("; ", tripRefs))}");
            }

            // Add final row for vehicle
            sb.AppendLine($"{CsvEscape(vehicle.PlateNumber)},{CsvEscape(vehicle.FuelType)},{vehicle.FuelTankCapacity:F2},FINAL,,,,,{currentLevel:F2},,");
        }

        return sb.ToString();
    }

    private string GenerateAllocationsCsv(
        List<VehicleFuelAllocation> allocations,
        List<Domain.Entities.Vehicle> vehicles,
        List<GasPurchase> purchases)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Allocation ID,Date,Vehicle,Vehicle Fuel Type,Purchase ID,Purchase Fuel Type,Liters Allocated,Cost (UZS),Purchase Total (L),Purchase Remaining (L),Notes");

        foreach (var alloc in allocations.OrderBy(a => a.AllocationDate).ThenBy(a => a.VehicleId))
        {
            var vehicle = vehicles.FirstOrDefault(v => v.Id == alloc.VehicleId);
            var purchase = purchases.FirstOrDefault(p => p.Id == alloc.GasPurchaseId);

            sb.AppendLine($"{alloc.Id},{alloc.AllocationDate:yyyy-MM-dd},{CsvEscape(vehicle?.PlateNumber ?? $"ID:{alloc.VehicleId}")},{CsvEscape(vehicle?.FuelType ?? "?")},{alloc.GasPurchaseId},{CsvEscape(purchase?.FuelType ?? "?")},{alloc.LitersAllocated:F2},{alloc.AllocationCostUzs:F0},{purchase?.LitersAmount ?? 0:F2},{purchase?.RemainingLiters ?? 0:F2},{CsvEscape(alloc.Notes ?? "")}");
        }

        // Totals
        sb.AppendLine();
        sb.AppendLine($"TOTAL,,,,,{allocations.Sum(a => a.LitersAllocated):F2},{allocations.Sum(a => a.AllocationCostUzs):F0},,,");

        return sb.ToString();
    }

    private string GeneratePurchasesCsv(List<GasPurchase> purchases)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Purchase ID,Date,Fuel Type,Liters Purchased,Liters Allocated,Remaining,Amount (UZS),Price/Liter,Status,Notes");

        foreach (var purchase in purchases.OrderBy(p => p.PurchaseDate))
        {
            string status = purchase.IsFullyAllocated ? "FULLY_ALLOCATED" :
                           (purchase.AllocatedLiters > 0 ? "PARTIAL" : "UNALLOCATED");

            sb.AppendLine($"{purchase.Id},{purchase.PurchaseDate:yyyy-MM-dd},{CsvEscape(purchase.FuelType)},{purchase.LitersAmount:F2},{purchase.AllocatedLiters:F2},{purchase.RemainingLiters:F2},{purchase.AmountUzs:F0},{purchase.PricePerLiter:F2},{status},{CsvEscape(purchase.Notes ?? "")}");
        }

        // Totals
        sb.AppendLine();
        sb.AppendLine($"TOTAL,,{purchases.Sum(p => p.LitersAmount):F2},{purchases.Sum(p => p.AllocatedLiters):F2},{purchases.Sum(p => p.RemainingLiters):F2},{purchases.Sum(p => p.AmountUzs):F0},,,");

        return sb.ToString();
    }

    /// <summary>
    /// Escape CSV values (handle commas, quotes, newlines)
    /// </summary>
    private string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // If contains comma, quote, or newline - wrap in quotes and escape internal quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    #endregion

    #region Private Helper Methods

    private List<VehicleFuelAllocation> AllocateFuelToVehicles(
        List<GasPurchase> purchases,
        List<VehicleFuelNeed> vehicles,
        int periodId,
        FuelCalculationResultDto result)
    {
        var allocations = new List<VehicleFuelAllocation>();
        double totalFuelNeeded = vehicles.Sum(v => v.FuelNeeded);

        if (totalFuelNeeded == 0)
        {
            result.Warnings.Add("No fuel consumption calculated (vehicles may have no trips)");
            return allocations;
        }

        double totalFuelAvailable = purchases.Sum(p => p.LitersAmount);
        
        Console.WriteLine($"\n  Total fuel needed by vehicles: {totalFuelNeeded:F1}L");
        Console.WriteLine($"  Total fuel available from purchases: {totalFuelAvailable:F1}L");

        foreach (var purchase in purchases)
        {
            double remainingFromPurchase = purchase.LitersAmount;

            // First pass: allocate to vehicles that still need fuel
            var vehiclesNeedingFuel = vehicles
                .Where(v => v.FuelNeeded > v.FuelAllocated + ALLOCATION_TOLERANCE)
                .OrderByDescending(v => v.FuelNeeded - v.FuelAllocated)
                .ToList();

            if (vehiclesNeedingFuel.Any())
            {
                double totalUnmetNeed = vehiclesNeedingFuel.Sum(v => v.FuelNeeded - v.FuelAllocated);

                foreach (var vehicleNeed in vehiclesNeedingFuel)
                {
                    if (remainingFromPurchase < ALLOCATION_TOLERANCE)
                        break;

                    double unmetNeed = vehicleNeed.FuelNeeded - vehicleNeed.FuelAllocated;
                    double proportion = totalUnmetNeed > 0
                        ? unmetNeed / totalUnmetNeed
                        : 1.0 / vehiclesNeedingFuel.Count;

                    double targetAllocation = remainingFromPurchase * proportion;
                    double actualAllocation = Math.Min(targetAllocation, unmetNeed);
                    actualAllocation = Math.Min(actualAllocation, remainingFromPurchase);

                    if (actualAllocation < ALLOCATION_TOLERANCE)
                        continue;

                    var allocation = new VehicleFuelAllocation
                    {
                        GasPurchaseId = purchase.Id,
                        VehicleId = vehicleNeed.Vehicle.Id,
                        ReportPeriodId = periodId,
                        LitersAllocated = Math.Round(actualAllocation, 2),
                        AllocationCostUzs = purchase.LitersAmount > 0
                            ? Math.Round(purchase.AmountUzs * (decimal)(actualAllocation / purchase.LitersAmount), 2)
                            : 0,
                        AllocationDate = purchase.PurchaseDate,
                        Reason = FuelAllocationReason.AutoDistanceBased,
                        Notes = $"Auto-allocated based on {vehicleNeed.TotalDistanceKm:F1}km driven",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    allocations.Add(allocation);
                    vehicleNeed.FuelAllocated += actualAllocation;
                    remainingFromPurchase -= actualAllocation;
                    purchase.AllocatedLiters += actualAllocation;

                    Console.WriteLine($"    → {vehicleNeed.Vehicle.PlateNumber}: +{actualAllocation:F1}L " +
                                      $"(total: {vehicleNeed.FuelAllocated:F1}L / {vehicleNeed.FuelNeeded:F1}L needed)");
                }
            }

            // Second pass: if there's still remaining fuel, try again without tolerance
            if (remainingFromPurchase > ALLOCATION_TOLERANCE)
            {
                var vehiclesStillNeedingFuel = vehicles
                    .Where(v => v.FuelNeeded > v.FuelAllocated)
                    .OrderByDescending(v => v.FuelNeeded - v.FuelAllocated)
                    .ToList();

                if (vehiclesStillNeedingFuel.Any())
                {
                    double totalUnmetNeed = vehiclesStillNeedingFuel.Sum(v => v.FuelNeeded - v.FuelAllocated);
                    
                    foreach (var vehicleNeed in vehiclesStillNeedingFuel)
                    {
                        if (remainingFromPurchase < ALLOCATION_TOLERANCE)
                            break;

                        double unmetNeed = vehicleNeed.FuelNeeded - vehicleNeed.FuelAllocated;
                        double proportion = totalUnmetNeed > 0 
                            ? unmetNeed / totalUnmetNeed 
                            : 1.0 / vehiclesStillNeedingFuel.Count;
                        
                        double extraAllocation = remainingFromPurchase * proportion;
                        extraAllocation = Math.Min(extraAllocation, unmetNeed);
                        extraAllocation = Math.Min(extraAllocation, remainingFromPurchase);

                        if (extraAllocation < ALLOCATION_TOLERANCE)
                            continue;

                        var allocation = new VehicleFuelAllocation
                        {
                            GasPurchaseId = purchase.Id,
                            VehicleId = vehicleNeed.Vehicle.Id,
                            ReportPeriodId = periodId,
                            LitersAllocated = Math.Round(extraAllocation, 2),
                            AllocationCostUzs = purchase.LitersAmount > 0
                                ? Math.Round(purchase.AmountUzs * (decimal)(extraAllocation / purchase.LitersAmount), 2)
                                : 0,
                            AllocationDate = purchase.PurchaseDate,
                            Reason = FuelAllocationReason.AutoDistanceBased,
                            Notes = $"Secondary allocation pass",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        allocations.Add(allocation);
                        vehicleNeed.FuelAllocated += extraAllocation;
                        remainingFromPurchase -= extraAllocation;
                        purchase.AllocatedLiters += extraAllocation;
                        
                        Console.WriteLine($"    → {vehicleNeed.Vehicle.PlateNumber}: +{extraAllocation:F1}L (secondary pass)");
                    }
                }
            }

            // Third pass: distribute any truly surplus fuel proportionally to all vehicles
            if (remainingFromPurchase > ALLOCATION_TOLERANCE)
            {
                var allVehicles = vehicles.OrderByDescending(v => v.FuelNeeded).ToList();
                double totalNeed = allVehicles.Sum(v => v.FuelNeeded);
                
                foreach (var vehicleNeed in allVehicles)
                {
                    if (remainingFromPurchase < ALLOCATION_TOLERANCE)
                        break;
                        
                    double proportion = totalNeed > 0 
                        ? vehicleNeed.FuelNeeded / totalNeed 
                        : 1.0 / allVehicles.Count;
                    double surplusAllocation = remainingFromPurchase * proportion;
                    
                    if (surplusAllocation < ALLOCATION_TOLERANCE)
                        continue;

                    var allocation = new VehicleFuelAllocation
                    {
                        GasPurchaseId = purchase.Id,
                        VehicleId = vehicleNeed.Vehicle.Id,
                        ReportPeriodId = periodId,
                        LitersAllocated = Math.Round(surplusAllocation, 2),
                        AllocationCostUzs = purchase.LitersAmount > 0
                            ? Math.Round(purchase.AmountUzs * (decimal)(surplusAllocation / purchase.LitersAmount), 2)
                            : 0,
                        AllocationDate = purchase.PurchaseDate,
                        Reason = FuelAllocationReason.AutoDistanceBased,
                        Notes = $"Surplus fuel allocation",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    allocations.Add(allocation);
                    vehicleNeed.FuelAllocated += surplusAllocation;
                    remainingFromPurchase -= surplusAllocation;
                    purchase.AllocatedLiters += surplusAllocation;
                    
                    Console.WriteLine($"    → {vehicleNeed.Vehicle.PlateNumber}: +{surplusAllocation:F1}L (surplus)");
                }
            }

            // Only warn if something truly couldn't be allocated
            if (remainingFromPurchase > ALLOCATION_TOLERANCE)
            {
                result.Warnings.Add($"{remainingFromPurchase:F1}L from {purchase.PurchaseDate:yyyy-MM-dd} could not be allocated");
            }
        }

        // Final validation - check for deficits
        foreach (var vehicleNeed in vehicles)
        {
            if (vehicleNeed.FuelAllocated < vehicleNeed.FuelNeeded - ALLOCATION_TOLERANCE)
            {
                double deficit = vehicleNeed.FuelNeeded - vehicleNeed.FuelAllocated;
                result.Warnings.Add($"{vehicleNeed.Vehicle.PlateNumber}: Fuel deficit of {deficit:F1}L " +
                                   $"(needed {vehicleNeed.FuelNeeded:F1}L, allocated {vehicleNeed.FuelAllocated:F1}L)");
            }
        }

        return allocations;
    }

    private bool TryParseDate(string value, out DateTime result)
    {
        var formats = new[]
        {
            "dd/MM/yyyy", "dd.MM.yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "d/M/yyyy", "d.M.yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out result))
                return true;
        }

        return DateTime.TryParse(value, out result);
    }

    /// <summary>
    /// Build purchase summary with SPECIFIC fuel type breakdown for reporting
    /// </summary>
    private GasPurchaseSummaryDto BuildPurchaseSummary(int periodId, IEnumerable<GasPurchase> purchases)
    {
        var purchaseList = purchases.ToList();
        
        // Group by SPECIFIC fuel type for detailed reporting
        var bySpecificType = purchaseList
            .GroupBy(p => p.SpecificFuelType ?? p.FuelType) // Fallback to FuelType if SpecificFuelType is null
            .Select(g => new FuelTypeSummaryDto
            {
                FuelType = g.Key,
                GenericFuelType = FuelTypeHelper.GetGenericFuelType(g.Key),
                PurchaseCount = g.Count(),
                TotalLiters = g.Sum(p => p.LitersAmount),
                TotalAmountUzs = g.Sum(p => p.AmountUzs),
                AveragePricePerLiter = g.Any() ? g.Average(p => p.PricePerLiter) : 0,
                AllocatedLiters = g.Sum(p => p.AllocatedLiters),
                RemainingLiters = g.Sum(p => p.RemainingLiters)
            })
            .OrderBy(f => f.GenericFuelType)
            .ThenBy(f => f.FuelType)
            .ToList();

        return new GasPurchaseSummaryDto
        {
            ReportPeriodId = periodId,
            TotalPurchases = purchaseList.Count,
            TotalLiters = purchaseList.Sum(p => p.LitersAmount),
            TotalAmountUzs = purchaseList.Sum(p => p.AmountUzs),
            AllocatedLiters = purchaseList.Sum(p => p.AllocatedLiters),
            RemainingLiters = purchaseList.Sum(p => p.RemainingLiters),
            ByFuelType = bySpecificType,
            Messages = new List<string>()
        };
    }

    private VehicleFuelStatusDto BuildVehicleFuelStatus(
        VehicleFuelNeed vehicleNeed,
        List<VehicleFuelAllocation> allocations,
        int periodId)
    {
        var vehicle = vehicleNeed.Vehicle;
        var totalAllocated = allocations.Sum(a => a.LitersAllocated);
        var totalCost = allocations.Sum(a => a.AllocationCostUzs);
        double endingLevel = vehicle.InitialFuelLevel + totalAllocated - vehicleNeed.FuelNeeded;

        var status = new VehicleFuelStatusDto
        {
            VehicleId = vehicle.Id,
            PlateNumber = vehicle.PlateNumber,
            Model = vehicle.Model,
            FuelType = vehicle.FuelType,
            TankCapacity = vehicle.FuelTankCapacity,
            ConsumptionPer100Km = vehicle.FuelConsumptionPer100Km,
            InitialFuelLevel = vehicle.InitialFuelLevel,
            TotalDistanceDriven = vehicleNeed.TotalDistanceKm,
            FuelConsumed = vehicleNeed.FuelNeeded,
            FuelAllocated = totalAllocated,
            CurrentFuelLevel = endingLevel,
            TotalFuelCostUzs = totalCost,
            Allocations = allocations.Select(a => new FuelAllocationDetailDto
            {
                Id = a.Id,
                AllocationDate = a.AllocationDate,
                LitersAllocated = a.LitersAllocated,
                CostUzs = a.AllocationCostUzs,
                FuelType = vehicle.FuelType,
                Reason = a.Reason.ToString(),
                Notes = a.Notes
            }).ToList()
        };

        if (endingLevel < 0)
        {
            status.Status = "NEGATIVE";
            status.Warnings.Add($"Fuel level goes negative ({endingLevel:F1}L) - insufficient fuel allocated");
        }
        else if (vehicle.FuelTankCapacity > 0 && endingLevel > vehicle.FuelTankCapacity)
        {
            status.Status = "OVER_CAPACITY";
            status.Warnings.Add($"Fuel level exceeds tank capacity ({endingLevel:F1}L > {vehicle.FuelTankCapacity}L)");
        }
        else if (endingLevel < MIN_FUEL_RESERVE)
        {
            status.Status = "LOW";
            status.Warnings.Add($"Low fuel warning: {endingLevel:F1}L remaining");
        }
        else
        {
            status.Status = "OK";
        }

        return status;
    }

    private class VehicleFuelNeed
    {
        public Domain.Entities.Vehicle Vehicle { get; set; } = null!;
        public double TotalDistanceKm { get; set; }
        public double FuelNeeded { get; set; }
        public double FuelAllocated { get; set; }
        public double CurrentFuelLevel { get; set; }
        public Dictionary<DateTime, List<Domain.Entities.Trip>> TripsByDate { get; set; } = new();
    }

    #endregion
}
