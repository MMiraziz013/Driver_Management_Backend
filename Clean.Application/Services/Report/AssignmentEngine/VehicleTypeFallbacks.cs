using Clean.Domain.Entities;

namespace Clean.Application.Services.Report.AssignmentEngine;

/// <summary>
/// Resolves which vehicle types a conflicting trip may be moved to.
/// </summary>
public static class VehicleTypeFallbacks
{
    /// <summary>
    /// Fallback types for a vehicle type in the order they should be tried.
    /// The original type is never included - it has already failed.
    /// </summary>
    public static List<int> GetCandidates(int vehicleTypeId, IEnumerable<VehicleTypeFallback> fallbacks)
    {
        return fallbacks
            .Where(f => f.VehicleTypeId == vehicleTypeId && f.FallbackVehicleTypeId != vehicleTypeId)
            .OrderBy(f => f.Priority)
            .Select(f => f.FallbackVehicleTypeId)
            .Distinct()
            .ToList();
    }
}
