using Clean.Domain.Entities;

namespace Clean.Application.Tests;

public class FuelConsumptionTests
{
    private static Vehicle CreateVehicle(double idlePerHour) => new()
    {
        FuelConsumptionPer100Km = 10,
        IdleFuelConsumptionPerHour = idlePerHour
    };

    private static Trip CreateTrip(double distanceKm, double? drivingHours, string garageOut, string garageIn) => new()
    {
        PickUpDate = new DateTime(2026, 10, 1),
        GarageOutTime = TimeSpan.Parse(garageOut),
        GarageInTime = TimeSpan.Parse(garageIn),
        DistanceKm = distanceKm,
        DrivingDurationHours = drivingHours
    };

    [Fact]
    public void Trip_AddsIdleFuelOnTopOfDrivingFuel()
    {
        var trip = CreateTrip(40, 1.5, "08:00", "18:00");

        // 40 km * 10 L/100km = 4 L driving, 8.5 idle hours * 2 L/h = 17 L idle
        Assert.Equal(8.5, trip.GetIdleHours(), 6);
        Assert.Equal(21, CreateVehicle(2).CalculateFuelConsumption(trip), 6);
    }

    [Fact]
    public void Trip_WithoutIdleRate_IsDistanceOnly()
    {
        var trip = CreateTrip(40, 1.5, "08:00", "18:00");

        Assert.Equal(4, CreateVehicle(0).CalculateFuelConsumption(trip), 6);
    }

    [Fact]
    public void Trip_WithoutRoutedDrivingTime_HasNoIdleFuel()
    {
        var trip = CreateTrip(150, null, "08:00", "18:00");

        Assert.Equal(0, trip.GetIdleHours());
        Assert.Equal(15, CreateVehicle(2).CalculateFuelConsumption(trip), 6);
    }

    [Fact]
    public void Trip_DrivingLongerThanDuration_HasNoNegativeIdle()
    {
        var trip = CreateTrip(100, 3, "08:00", "10:00");

        Assert.Equal(0, trip.GetIdleHours());
        Assert.Equal(10, CreateVehicle(2).CalculateFuelConsumption(trip), 6);
    }

    [Fact]
    public void Trip_OvernightDuration_IsCountedAcrossMidnight()
    {
        var trip = CreateTrip(20, 1, "22:00", "02:00");

        Assert.Equal(3, trip.GetIdleHours(), 6);
    }

    [Fact]
    public void Trips_AreSummedPerTrip()
    {
        var trips = new[]
        {
            CreateTrip(40, 1.5, "08:00", "18:00"),
            CreateTrip(150, null, "08:00", "18:00")
        };

        Assert.Equal(36, CreateVehicle(2).CalculateFuelConsumption(trips), 6);
    }
}
