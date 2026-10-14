using Clean.Application.Dtos.Fuel;
using Clean.Application.Services.Gas;
using Clean.Domain.Entities;

namespace Clean.Application.Tests;

public class DailyFuelAllocatorTests
{
    private static readonly DateTime Day = new(2026, 10, 1);

    private static VehicleFuelTracker Tracker(int id, double tank, double level, string fuelType = "АИ-92") => new()
    {
        Vehicle = new Vehicle
        {
            Id = id,
            PlateNumber = $"V{id}",
            FuelTankCapacity = tank,
            FuelType = fuelType,
            FuelConsumptionPer100Km = 10
        },
        CurrentFuelLevel = level
    };

    private static GasPurchase Purchase(int id, double liters, string fuelType = "АИ-92") => new()
    {
        Id = id,
        PurchaseDate = Day,
        LitersAmount = liters,
        AmountUzs = (decimal)liters * 10000,
        FuelType = fuelType
    };

    private static (List<VehicleFuelAllocation> Allocations, FuelCalculationResultDto Result) Run(
        List<GasPurchase> purchases, params VehicleFuelTracker[] trackers)
    {
        var result = new FuelCalculationResultDto();
        var allocations = new DailyFuelAllocator().Allocate(
            purchases, trackers.ToDictionary(t => t.Vehicle.Id), 1, result);
        return (allocations, result);
    }

    [Fact]
    public void LargePurchase_SmallRemainder_IsAllocatedToNextVehicle()
    {
        // 40L purchase, first car has room for 37L: the last 3L used to be dropped by the 5L tolerance
        var purchase = Purchase(1, 40);
        var (allocations, result) = Run([purchase], Tracker(1, 50, 13), Tracker(2, 50, 30));

        Assert.Equal(40, allocations.Sum(a => a.LitersAllocated), 2);
        Assert.Equal(40, purchase.AllocatedLiters, 2);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void SmallPurchase_GoesToVehicleThatCanHoldIt()
    {
        // Lowest level is V1 (5L in a 25L tank, 20L room); V2 has 20L in a 60L tank (40L room)
        var purchase = Purchase(1, 25);
        var (allocations, result) = Run([purchase], Tracker(1, 25, 5), Tracker(2, 60, 20));

        var allocation = Assert.Single(allocations);
        Assert.Equal(2, allocation.VehicleId);
        Assert.Equal(25, allocation.LitersAllocated, 2);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void WhenSeveralVehiclesFit_LowestFuelLevelWins()
    {
        var (allocations, _) = Run([Purchase(1, 20)], Tracker(1, 60, 30), Tracker(2, 60, 10));

        Assert.Equal(2, Assert.Single(allocations).VehicleId);
    }

    [Fact]
    public void FullVehicle_DoesNotStopAllocationToOthers()
    {
        var purchase = Purchase(1, 50);
        var (allocations, result) = Run([purchase], Tracker(1, 50, 50), Tracker(2, 50, 10), Tracker(3, 50, 30));

        Assert.DoesNotContain(allocations, a => a.VehicleId == 1);
        Assert.Equal(50, allocations.Sum(a => a.LitersAllocated), 2);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void AllTanksFull_LeavesLeftoverWithReason()
    {
        var purchase = Purchase(1, 40);
        var (allocations, result) = Run([purchase], Tracker(1, 50, 35), Tracker(2, 50, 45));

        Assert.Equal(20, allocations.Sum(a => a.LitersAllocated), 2);
        Assert.Contains(result.Warnings, w => w.Contains("20.00L") && w.Contains("all compatible tanks full"));
    }

    [Fact]
    public void SmallPurchase_ThatFitsNoTank_GoesToMostRoomAndReportsRest()
    {
        var purchase = Purchase(1, 25);
        var (allocations, result) = Run([purchase], Tracker(1, 50, 35), Tracker(2, 50, 30));

        var allocation = Assert.Single(allocations);
        Assert.Equal(2, allocation.VehicleId);
        Assert.Equal(20, allocation.LitersAllocated, 2);
        Assert.Contains(result.Warnings, w => w.Contains("5.00L") && w.Contains("small purchase did not fit any single tank"));
    }

    [Fact]
    public void IncompatibleFuel_IsReported()
    {
        var (allocations, result) = Run([Purchase(1, 40, "ДТ")], Tracker(1, 50, 10));

        Assert.Empty(allocations);
        Assert.Contains(result.Warnings, w => w.Contains("no compatible vehicle"));
    }

    [Fact]
    public void ManyPurchases_NeverOverfill_AndBalance()
    {
        var purchases = new List<GasPurchase>
        {
            Purchase(1, 57.3), Purchase(2, 18.4), Purchase(3, 33.33), Purchase(4, 9.99), Purchase(5, 44.1)
        };
        var trackers = new[] { Tracker(1, 45, 3.7), Tracker(2, 60, 12.25), Tracker(3, 70, 41.9) };
        var startLevels = trackers.ToDictionary(t => t.Vehicle.Id, t => t.CurrentFuelLevel);

        var (allocations, result) = Run(purchases, trackers);

        foreach (var tracker in trackers)
        {
            var received = allocations.Where(a => a.VehicleId == tracker.Vehicle.Id).Sum(a => a.LitersAllocated);
            Assert.True(startLevels[tracker.Vehicle.Id] + received
                        <= tracker.Vehicle.FuelTankCapacity + DailyFuelAllocator.ALLOCATION_EPSILON);
        }

        var purchased = purchases.Sum(p => p.LitersAmount);
        var allocated = allocations.Sum(a => a.LitersAllocated);
        var leftover = purchases.Sum(p => p.LitersAmount - p.AllocatedLiters);
        Assert.Equal(purchased, allocated + leftover, 6);
        Assert.Equal(allocated, purchases.Sum(p => p.AllocatedLiters), 6);
    }
}
