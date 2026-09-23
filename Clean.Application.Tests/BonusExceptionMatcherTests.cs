using Clean.Application.Services.Bonus;
using Clean.Application.Services.Report;
using Clean.Domain.Entities;
using Clean.Domain.Enums;

namespace Clean.Application.Tests;

public class BonusExceptionMatcherTests
{
    private const int RoundTripId = 5;
    private const string ChirchikRoute =
        "PU: Rayyan Hotel (41.357045, 69.345927),, 100071 Temur Malik ko'chasi, Tashkent ; " +
        "ST: Chirchik (41.461059, 69.575292) ; " +
        "DO: Rayyan Hotel (41.357045, 69.345927),, 100071 Temur Malik ko'chasi, Tashkent ";
    private const string TashkentRoute =
        "PU: Rayyan Hotel (41.357045, 69.345927), Tashkent ; DO: Hyatt Regency, Tashkent";

    private static BonusCalculationException SierraNevadaRule(List<string>? vehicleTypes = null) => new()
    {
        Name = "Sierra Nevada - Chirchik",
        IsActive = true,
        CompanyNameContains = "Sierra Nevada",
        ServiceTypeId = RoundTripId,
        CalculationMethod = BonusCalculationMethod.DurationBased,
        VehicleTypes = vehicleTypes ?? new List<string>(),
        LocationKeywords = new List<string> { "Chirchik", "Chirchiq" }
    };

    private static Trip MakeTrip(
        string company = "Sierra Nevada LLC",
        int serviceTypeId = RoundTripId,
        string vehicleType = "Sedan",
        string routing = ChirchikRoute) => new()
    {
        CompanyName = company,
        ServiceTypeId = serviceTypeId,
        ImportedVehicleType = vehicleType,
        RoutingDetails = routing
    };

    [Fact]
    public void RouteContainsAny_ExampleChirchikRoute_ReturnsTrue()
    {
        Assert.True(RoutingDetailsParser.RouteContainsAny(ChirchikRoute, new[] { "chirchik" }));
    }

    [Fact]
    public void RouteContainsAny_KeywordAfterCommaInAddress_ReturnsTrue()
    {
        var route = "PU: Hotel X, Chirchik ; DO: Rayyan Hotel, Tashkent";
        Assert.True(RoutingDetailsParser.RouteContainsAny(route, new[] { "Chirchik" }));
    }

    [Fact]
    public void RouteContainsAny_TashkentOnlyRoute_ReturnsFalse()
    {
        Assert.False(RoutingDetailsParser.RouteContainsAny(TashkentRoute, new[] { "Chirchik", "Chirchiq" }));
    }

    [Theory]
    [InlineData("Sedan")]
    [InlineData("Luxury minibus")]
    [InlineData("SUV")]
    public void Matches_EmptyVehicleTypes_MatchesAnyVehicle(string vehicleType)
    {
        Assert.True(BonusExceptionMatcher.Matches(MakeTrip(vehicleType: vehicleType), SierraNevadaRule()));
    }

    [Fact]
    public void Matches_ListedVehicleTypes_OnlyMatchesListed()
    {
        var rule = SierraNevadaRule(new List<string> { "Luxury minibus" });

        Assert.True(BonusExceptionMatcher.Matches(MakeTrip(vehicleType: "luxury minibus"), rule));
        Assert.False(BonusExceptionMatcher.Matches(MakeTrip(vehicleType: "Sedan"), rule));
    }

    [Fact]
    public void Matches_RouteWithoutChirchik_DoesNotMatch()
    {
        Assert.False(BonusExceptionMatcher.Matches(MakeTrip(routing: TashkentRoute), SierraNevadaRule()));
    }

    [Fact]
    public void Matches_OtherCompanyOrServiceType_DoesNotMatch()
    {
        var rule = SierraNevadaRule();

        Assert.False(BonusExceptionMatcher.Matches(MakeTrip(company: "Other Corp"), rule));
        Assert.False(BonusExceptionMatcher.Matches(MakeTrip(serviceTypeId: RoundTripId + 1), rule));
    }

    [Fact]
    public void Matches_InactiveRule_DoesNotMatch()
    {
        var rule = SierraNevadaRule();
        rule.IsActive = false;

        Assert.False(BonusExceptionMatcher.Matches(MakeTrip(), rule));
    }
}
