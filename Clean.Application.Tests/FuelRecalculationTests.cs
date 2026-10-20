using Clean.Application.Dtos.Fuel;
using Clean.Application.Services.Gas;
using Clean.Domain.Entities;

namespace Clean.Application.Tests;

public class FuelRecalculationTests
{
    private static readonly DateTime Day1 = new(2026, 10, 1);
    private static readonly DateTime Day2 = new(2026, 10, 2);

    private static Vehicle Car(int id, double tank, double initial, double per100Km) => new()
    {
        Id = id,
        PlateNumber = $"V{id}",
        FuelTankCapacity = tank,
        InitialFuelLevel = initial,
        FuelType = "АИ-92",
        FuelConsumptionPer100Km = per100Km
    };

    private static Dictionary<DateTime, List<Trip>> Trips(DateTime date, double km) => new()
    {
        [date] = [new Trip { PickUpDate = date, DistanceKm = km }]
    };

    private static VehicleFuelAllocation Fill(DateTime date, double liters) => new()
    {
        AllocationDate = date,
        LitersAllocated = liters
    };

    private static GasPurchase Purchase(int id, DateTime date, double liters, double alreadyAllocated = 0) => new()
    {
        Id = id,
        PurchaseDate = date,
        LitersAmount = liters,
        AllocatedLiters = alreadyAllocated,
        AmountUzs = (decimal)liters * 10000,
        FuelType = "АИ-92"
    };

    [Fact]
    public void LoweredConsumption_OverflowsOnFillDay()
    {
        // Filled 40L after a 400km trip at 10L/100km (tank 40L, from 30L: 30 - 40 + 40 = 30L).
        // At 5L/100km the trip only uses 20L, so the same fill takes the tank to 10 + 40 = 50L.
        var car = Car(1, tank: 40, initial: 30, per100Km: 5);

        var violations = FuelLevelSimulator.FindViolations(car, Trips(Day1, 400), [Fill(Day1, 40)]);

        var violation = Assert.Single(violations);
        Assert.Equal(FuelViolationKind.Overflow, violation.Kind);
        Assert.Equal(Day1, violation.Date);
        Assert.Equal(50, violation.Level, 2);
    }

    [Fact]
    public void RaisedConsumption_RunsIntoDeficit()
    {
        // At 20L/100km a 400km trip needs 80L: 10 - 80 + 40 = -30L
        var car = Car(1, tank: 50, initial: 10, per100Km: 20);

        var violation = Assert.Single(FuelLevelSimulator.FindViolations(car, Trips(Day1, 400), [Fill(Day1, 40)]));

        Assert.Equal(FuelViolationKind.Deficit, violation.Kind);
        Assert.Equal(-30, violation.Level, 2);
    }

    [Fact]
    public void ConsistentVehicle_HasNoViolations()
    {
        var car = Car(1, tank: 50, initial: 10, per100Km: 10);

        Assert.Empty(FuelLevelSimulator.FindViolations(car, Trips(Day1, 400), [Fill(Day1, 40)]));
        Assert.Equal(10, FuelLevelSimulator.EndingLevel(car, Trips(Day1, 400), [Fill(Day1, 40)]), 2);
    }

    [Fact]
    public void FixedAdditions_ReduceRoomInTank()
    {
        // 30L tank at 5L; a kept 20L manual fill leaves 5L room, so a 20L purchase no longer fits
        var tracker = new VehicleFuelTracker
        {
            Vehicle = Car(1, tank: 30, initial: 5, per100Km: 10),
            CurrentFuelLevel = 5,
            FixedAdditionsByDate = { [Day1] = 20 }
        };
        var purchase = Purchase(1, Day1, 20);
        var result = new FuelCalculationResultDto();

        var allocations = new DailyFuelAllocator().Allocate([purchase], new() { [1] = tracker }, 1, result);

        var allocation = Assert.Single(allocations);
        Assert.Equal(5, allocation.LitersAllocated, 2);
        Assert.Equal(30, tracker.CurrentFuelLevel, 2);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void SelectedVehicle_OnlyGetsTheFreeRemainderOfAPurchase()
    {
        // 40L purchase, 30L already belongs to a vehicle that is not being recalculated
        var tracker = new VehicleFuelTracker { Vehicle = Car(2, tank: 60, initial: 0, per100Km: 10) };
        var purchase = Purchase(1, Day2, 40, alreadyAllocated: 30);

        var allocations = new DailyFuelAllocator().Allocate([purchase], new() { [2] = tracker }, 1, new FuelCalculationResultDto());

        var allocation = Assert.Single(allocations);
        Assert.Equal(10, allocation.LitersAllocated, 2);
        Assert.Equal(40, purchase.AllocatedLiters, 2);
    }
}
