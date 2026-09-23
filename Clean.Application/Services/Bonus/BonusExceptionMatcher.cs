using Clean.Application.Services.Report;
using Clean.Domain.Entities;

namespace Clean.Application.Services.Bonus;

public static class BonusExceptionMatcher
{
    /// <summary>
    /// The trip's vehicle type as imported (e.g. "Luxury minibus").
    /// Falls back to the stored VehicleType for trips imported before ImportedVehicleType existed.
    /// </summary>
    public static string? GetTripVehicleType(Domain.Entities.Trip trip) =>
        !string.IsNullOrWhiteSpace(trip.ImportedVehicleType)
            ? trip.ImportedVehicleType.Trim()
            : trip.VehicleType?.Name?.Trim();

    /// <summary>
    /// Returns the first exception rule the trip falls under, or null.
    /// Empty vehicle type / location lists mean "any".
    /// </summary>
    public static BonusCalculationException? FindMatch(
        Domain.Entities.Trip trip,
        IEnumerable<BonusCalculationException> exceptions)
    {
        return exceptions.FirstOrDefault(e => Matches(trip, e));
    }

    public static bool Matches(Domain.Entities.Trip trip, BonusCalculationException exception)
    {
        if (!exception.IsActive)
            return false;

        if (trip.ServiceTypeId != exception.ServiceTypeId)
            return false;

        if (string.IsNullOrWhiteSpace(exception.CompanyNameContains) ||
            !(trip.CompanyName?.Contains(exception.CompanyNameContains.Trim(), StringComparison.OrdinalIgnoreCase) ?? false))
            return false;

        var vehicleTypes = exception.VehicleTypes;
        if (vehicleTypes.Count > 0)
        {
            var tripVehicleType = GetTripVehicleType(trip);
            if (tripVehicleType == null ||
                !vehicleTypes.Any(v => v.Trim().Equals(tripVehicleType, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        var locationKeywords = exception.LocationKeywords;
        if (locationKeywords.Count > 0 &&
            !RoutingDetailsParser.RouteContainsAny(trip.RoutingDetails, locationKeywords))
            return false;

        return true;
    }
}
