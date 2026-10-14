using Clean.Application.Dtos.Fuel;
using Clean.Domain.Entities;

namespace Clean.Application.Services.Gas;

/// <summary>
/// Tracking class for daily fuel simulation
/// </summary>
public class VehicleFuelTracker
{
    public Domain.Entities.Vehicle Vehicle { get; set; } = null!;
    public double TotalDistanceKm { get; set; }
    public double TotalFuelNeeded { get; set; }
    public double CurrentFuelLevel { get; set; }      // Changes daily as we simulate
    public double FuelConsumedSoFar { get; set; }     // Running total of fuel used
    public double TotalFuelAllocated { get; set; }    // Running total of fuel received
    public Dictionary<DateTime, List<Domain.Entities.Trip>> TripsByDate { get; set; } = new();

    public double RoomInTank => Math.Max(0, Vehicle.FuelTankCapacity - CurrentFuelLevel);
}

/// <summary>
/// Daily fuel allocation algorithm.
/// Processes day-by-day, deducting fuel for trips, then allocating purchases made that day.
///
/// Rules:
/// - Purchases ≤ 29.9L → ONE vehicle only (no splitting)
/// - Purchases > 29.9L → CAN be split across multiple vehicles if needed
/// - No overfueling (cannot exceed tank capacity)
/// - A purchase goes to a vehicle that can take all of it when one exists (lowest fuel level first),
///   otherwise to the vehicle with the most room in its tank
/// - АИ-92 and АИ-95 are interchangeable
/// </summary>
public class DailyFuelAllocator
{
    /// <summary>
    /// Smallest amount (liters) treated as "something left". Below this, a purchase counts as fully allocated.
    /// </summary>
    public const double ALLOCATION_EPSILON = 0.01;

    /// <summary>
    /// Threshold for allowing purchase splitting across multiple vehicles.
    /// Purchases larger than this can be split; smaller ones go to one vehicle only.
    /// </summary>
    public const double LARGE_PURCHASE_THRESHOLD = 29.9;

    /// <summary>
    /// Ending fuel deficits smaller than this (liters) are not reported
    /// </summary>
    public const double DEFICIT_WARNING_THRESHOLD = 5.0;

    public List<VehicleFuelAllocation> Allocate(
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
                        double fuelUsed = tracker.Vehicle.CalculateFuelConsumption(trip);

                        tracker.CurrentFuelLevel -= fuelUsed;
                        tracker.FuelConsumedSoFar += fuelUsed;

                        Console.WriteLine($"  🚗 {tracker.Vehicle.PlateNumber}: Trip {trip.DistanceKm ?? 0:F1}km, " +
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
                AllocatePurchase(purchase, date, vehicleTrackers, periodId, allocations, result);
            }
        }

        // Final check - report vehicles with negative fuel (deficit)
        Console.WriteLine($"\n=== FINAL FUEL STATUS ===");
        foreach (var tracker in vehicleTrackers.Values)
        {
            string status = tracker.CurrentFuelLevel >= 0 ? "✓" : "⚠️ DEFICIT";
            Console.WriteLine($"  {status} {tracker.Vehicle.PlateNumber}: {tracker.CurrentFuelLevel:F1}L " +
                              $"(consumed: {tracker.FuelConsumedSoFar:F1}L, allocated: {tracker.TotalFuelAllocated:F1}L)");

            if (tracker.CurrentFuelLevel < -DEFICIT_WARNING_THRESHOLD)
            {
                result.Warnings.Add($"{tracker.Vehicle.PlateNumber}: Fuel deficit of {Math.Abs(tracker.CurrentFuelLevel):F1}L " +
                                    $"(insufficient fuel purchases to cover {tracker.FuelConsumedSoFar:F1}L consumed)");
            }
        }

        return allocations;
    }

    private static void AllocatePurchase(
        GasPurchase purchase,
        DateTime date,
        Dictionary<int, VehicleFuelTracker> vehicleTrackers,
        int periodId,
        List<VehicleFuelAllocation> allocations,
        FuelCalculationResultDto result)
    {
        double remainingInPurchase = purchase.LitersAmount - purchase.AllocatedLiters;

        if (remainingInPurchase <= ALLOCATION_EPSILON)
            return;

        bool isLargePurchase = purchase.LitersAmount > LARGE_PURCHASE_THRESHOLD;
        bool isFirstAllocationForThisPurchase = true;

        Console.WriteLine($"    Processing: {remainingInPurchase:F1}L {purchase.FuelType} " +
                          $"(splittable: {(isLargePurchase ? "YES" : "NO")})");

        // Loop to potentially allocate to multiple vehicles (only if large purchase)
        while (remainingInPurchase > ALLOCATION_EPSILON)
        {
            // Find compatible vehicles that can accept fuel
            var compatibleVehicles = vehicleTrackers.Values
                .Where(v => AreFuelTypesCompatible(v.Vehicle.FuelType, purchase.FuelType))
                .ToList();

            var vehiclesWithRoom = compatibleVehicles
                .Where(v => v.RoomInTank > ALLOCATION_EPSILON)
                .ToList();

            if (!vehiclesWithRoom.Any())
            {
                string reason = compatibleVehicles.Any()
                    ? "all compatible tanks full"
                    : "no compatible vehicle";

                AddLeftoverWarning(result, purchase, date, remainingInPurchase, reason);
                break;
            }

            // Prefer a vehicle that can take the whole remainder (no split), lowest fuel level first.
            // Otherwise fill the vehicle with the most room.
            var vehiclesThatFit = vehiclesWithRoom
                .Where(v => v.RoomInTank >= remainingInPurchase - ALLOCATION_EPSILON)
                .ToList();

            var targetVehicle = vehiclesThatFit.Any()
                ? vehiclesThatFit
                    .OrderBy(v => v.CurrentFuelLevel) // Lowest fuel level first (most urgent)
                    .ThenByDescending(v => v.TotalFuelNeeded) // Tie-breaker: vehicles that need more overall
                    .First()
                : vehiclesWithRoom
                    .OrderByDescending(v => v.RoomInTank)
                    .ThenBy(v => v.CurrentFuelLevel)
                    .First();

            double tankCapacity = targetVehicle.Vehicle.FuelTankCapacity;
            double currentLevel = targetVehicle.CurrentFuelLevel;

            // Can only fill up to tank capacity - NO OVERFUELING
            double toAllocate = remainingInPurchase <= targetVehicle.RoomInTank + ALLOCATION_EPSILON
                ? Math.Round(remainingInPurchase, 2)
                : Math.Floor(targetVehicle.RoomInTank * 100) / 100;

            if (toAllocate < ALLOCATION_EPSILON)
                break; // Only possible when the remainder itself rounds to 0

            // Create allocation
            var allocation = new VehicleFuelAllocation
            {
                GasPurchaseId = purchase.Id,
                VehicleId = targetVehicle.Vehicle.Id,
                ReportPeriodId = periodId,
                LitersAllocated = toAllocate,
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
                if (remainingInPurchase > ALLOCATION_EPSILON)
                {
                    AddLeftoverWarning(result, purchase, date, remainingInPurchase,
                        "small purchase did not fit any single tank");
                }
                break;
            }

            // For large purchases, continue loop to try allocating remainder to other vehicles
        }
    }

    private static void AddLeftoverWarning(
        FuelCalculationResultDto result,
        GasPurchase purchase,
        DateTime date,
        double liters,
        string reason)
    {
        result.Warnings.Add($"{liters:F2}L of {date:yyyy-MM-dd} {purchase.FuelType} purchase " +
                            $"({purchase.LitersAmount:F2}L) not allocated: {reason}");
    }

    /// <summary>
    /// Check if fuel types are compatible:
    /// - Gasoline types (АИ-92, АИ-95) are interchangeable
    /// - Diesel only matches Diesel
    /// </summary>
    public static bool AreFuelTypesCompatible(string vehicleFuelType, string purchaseFuelType)
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
}
