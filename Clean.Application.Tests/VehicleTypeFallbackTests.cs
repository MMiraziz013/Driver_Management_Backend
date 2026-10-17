using Clean.Application.Services.Report.AssignmentEngine;
using Clean.Domain.Entities;

namespace Clean.Application.Tests;

public class VehicleTypeFallbackTests
{
    private const int Sedan = 1, Suv = 2, Sonata = 3, Tucson = 4, Minivan = 5, Minibus = 6, LuxuryMinibus = 7;

    private static readonly List<VehicleTypeFallback> Chains =
    [
        Fallback(Suv, Tucson, 3),
        Fallback(Suv, Sedan, 1),
        Fallback(Suv, Sonata, 2),
        Fallback(Minivan, Minibus, 1),
        Fallback(Minivan, LuxuryMinibus, 2),
        Fallback(LuxuryMinibus, Minibus, 1)
    ];

    private static VehicleTypeFallback Fallback(int from, int to, int priority) => new()
    {
        VehicleTypeId = from,
        FallbackVehicleTypeId = to,
        Priority = priority
    };

    private static Vehicle Car(int id, int typeId) => new()
    {
        Id = id,
        PlateNumber = $"V{id}",
        VehicleTypeId = typeId
    };

    private static Trip TripOf(int typeId, int startHour, int endHour) => new()
    {
        Id = 100 + startHour,
        PickUpDate = new DateTime(2026, 10, 1),
        GarageOutTime = TimeSpan.FromHours(startHour),
        GarageInTime = TimeSpan.FromHours(endHour),
        VehicleTypeId = typeId
    };

    [Fact]
    public void Candidates_AreOrderedByPriority()
    {
        Assert.Equal([Sedan, Sonata, Tucson], VehicleTypeFallbacks.GetCandidates(Suv, Chains));
        Assert.Equal([Minibus, LuxuryMinibus], VehicleTypeFallbacks.GetCandidates(Minivan, Chains));
    }

    [Fact]
    public void LuxuryMinibus_NeverFallsBackToMinivan()
    {
        Assert.Equal([Minibus], VehicleTypeFallbacks.GetCandidates(LuxuryMinibus, Chains));
    }

    [Fact]
    public void TypeWithoutChain_HasNoCandidates()
    {
        Assert.Empty(VehicleTypeFallbacks.GetCandidates(Sedan, Chains));
    }

    [Fact]
    public void Candidates_NeverIncludeTheOriginalType()
    {
        var chains = new List<VehicleTypeFallback> { Fallback(Suv, Suv, 1), Fallback(Suv, Sedan, 2) };

        Assert.Equal([Sedan], VehicleTypeFallbacks.GetCandidates(Suv, chains));
    }

    [Fact]
    public void AvailableVehicles_UseOverrideTypeAndSkipBusyOnes()
    {
        var filters = new VehicleFilters(new DriverFilters(), null!);
        var vehicles = new List<Vehicle> { Car(1, Suv), Car(2, Sedan), Car(3, Sedan) };

        // V2 is already on an overlapping trip
        var busyTrip = TripOf(Sedan, 9, 12);
        var existing = new List<DriverAssignment> { new() { VehicleId = 2, Trip = busyTrip, TripId = busyTrip.Id } };

        var trip = TripOf(Suv, 10, 11);
        var workload = vehicles.ToDictionary(v => v.Id, _ => 0);

        Assert.Equal([1], filters.GetAvailableVehicles(trip, vehicles, existing, workload).Select(v => v.Id));
        Assert.Equal([3], filters.GetAvailableVehicles(trip, vehicles, existing, workload, Sedan).Select(v => v.Id));
    }
}
