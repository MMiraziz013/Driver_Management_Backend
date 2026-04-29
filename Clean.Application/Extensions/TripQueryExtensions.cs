using Clean.Domain.Entities;

namespace Clean.Application.Extensions;

public static class TripQueryExtensions
{
    // Add all variations of "Cash" payment that might appear in your data
    private static readonly string[] CashPaymentIndicators = 
    { 
        "Cash", "cash", "CASH",
        "Наличные", "наличные", "НАЛИЧНЫЕ",
        "Naqd", "naqd", "NAQD"
    };
    
    /// <summary>
    /// Filters out cash trips from the query.
    /// Use this in all services EXCEPT bonus calculation.
    /// </summary>
    public static IQueryable<Trip> ExcludeCashTrips(this IQueryable<Trip> query)
    {
        return query.Where(t => 
            t.PmtMethod == null || 
            t.PmtMethod == "" ||
            !CashPaymentIndicators.Any(indicator => t.PmtMethod.Contains(indicator)));
    }
    
    /// <summary>
    /// Filters out cash trips from an in-memory collection.
    /// </summary>
    public static IEnumerable<Trip> ExcludeCashTrips(this IEnumerable<Trip> trips)
    {
        return trips.Where(t => !IsCashTrip(t));
    }
    
    /// <summary>
    /// Checks if a trip is a cash trip.
    /// </summary>
    public static bool IsCashTrip(Trip trip)
    {
        if (string.IsNullOrEmpty(trip.PmtMethod))
            return false;
            
        return CashPaymentIndicators.Any(indicator => 
            trip.PmtMethod.Contains(indicator, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Parses vehicle model from ImportedVehiclePlate.
    /// Example: "Toyota Hiace(01/174FHA)" -> "Toyota Hiace"
    /// </summary>
    public static string ParseVehicleModel(string? importedVehiclePlate)
    {
        if (string.IsNullOrEmpty(importedVehiclePlate))
            return "Unknown";
            
        var parenIndex = importedVehiclePlate.IndexOf('(');
        if (parenIndex > 0)
        {
            return importedVehiclePlate.Substring(0, parenIndex).Trim();
        }
        
        return importedVehiclePlate.Trim();
    }
}