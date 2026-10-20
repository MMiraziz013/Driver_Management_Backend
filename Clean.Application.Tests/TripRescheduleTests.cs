using Clean.Application.Services.Report.AssignmentEngine;
using Clean.Domain.Entities;

namespace Clean.Application.Tests;

public class TripRescheduleTests
{
    private static readonly DateTime PeriodStart = new(2026, 10, 1);
    private static readonly DateTime PeriodEnd = new(2026, 10, 15);

    private static DateTime Day(int day) => new(2026, 10, day);

    [Fact]
    public void Candidates_AreClosestFirst_EarlierOnTie()
    {
        var dates = TripReschedule.GetCandidateDates(Day(8), PeriodStart, PeriodEnd);

        Assert.Equal([Day(7), Day(9), Day(6), Day(10), Day(5), Day(11)], dates.Take(6));
    }

    [Fact]
    public void Candidates_CoverEveryOtherDayOfThePeriod()
    {
        var dates = TripReschedule.GetCandidateDates(Day(2), PeriodStart, PeriodEnd);

        // Day 1 is the only earlier day, then the rest of the period in order up to the last day
        Assert.Equal(14, dates.Count);
        Assert.Equal([Day(1), Day(3), Day(4)], dates.Take(3));
        Assert.Equal(Day(15), dates.Last());
    }

    [Fact]
    public void Candidates_StayInsidePeriod()
    {
        Assert.All(TripReschedule.GetCandidateDates(Day(15), PeriodStart, PeriodEnd),
            d => Assert.InRange(d, PeriodStart, PeriodEnd));
        Assert.Equal([Day(14), Day(13), Day(12)], TripReschedule.GetCandidateDates(Day(15), PeriodStart, PeriodEnd).Take(3));
    }

    [Fact]
    public void Candidates_NeverIncludeOriginalDay()
    {
        Assert.DoesNotContain(Day(8), TripReschedule.GetCandidateDates(Day(8), PeriodStart, PeriodEnd));
    }

    [Fact]
    public void DriverOnLeave_BecomesAvailableWhenTripMovesOffTheLeaveDay()
    {
        // Reschedule relies on the filters reading the trip's own date
        var filters = new DriverFilters();
        var driver = new Driver
        {
            Id = 1,
            Vacations = [new DriverVacation { StartDate = Day(8), EndDate = Day(8) }]
        };
        var trip = new Trip { PickUpDate = Day(8), GarageOutTime = TimeSpan.FromHours(9), GarageInTime = TimeSpan.FromHours(12) };

        Assert.True(filters.IsOnLeave(driver, trip.PickUpDate));

        trip.PickUpDate = Day(9);

        Assert.False(filters.IsOnLeave(driver, trip.PickUpDate));
        Assert.Equal(Day(9).AddHours(9), trip.GetStartDateTime());
    }
}
