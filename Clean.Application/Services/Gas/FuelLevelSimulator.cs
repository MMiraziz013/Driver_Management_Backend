using Clean.Domain.Entities;

namespace Clean.Application.Services.Gas;

public enum FuelViolationKind
{
    Overflow,
    Deficit
}

public record FuelViolation(DateTime Date, FuelViolationKind Kind, double Level);

/// <summary>
/// Replays a vehicle's tank day by day with its current settings and saved allocations,
/// in the same order the allocator uses: the day's trips first, then the day's fuel.
/// </summary>
public static class FuelLevelSimulator
{
    public static List<FuelViolation> FindViolations(
        Domain.Entities.Vehicle vehicle,
        Dictionary<DateTime, List<Domain.Entities.Trip>> tripsByDate,
        IEnumerable<VehicleFuelAllocation> allocations)
    {
        var litersByDate = allocations
            .GroupBy(a => a.AllocationDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.LitersAllocated));

        var violations = new List<FuelViolation>();
        var level = vehicle.InitialFuelLevel;

        foreach (var date in tripsByDate.Keys.Concat(litersByDate.Keys).Distinct().OrderBy(d => d))
        {
            if (tripsByDate.TryGetValue(date, out var trips))
                level -= trips.Sum(vehicle.CalculateFuelConsumption);

            if (litersByDate.TryGetValue(date, out var liters))
                level += liters;

            if (vehicle.FuelTankCapacity > 0 && level > vehicle.FuelTankCapacity + DailyFuelAllocator.ALLOCATION_EPSILON)
                violations.Add(new FuelViolation(date, FuelViolationKind.Overflow, Math.Round(level, 2)));
            else if (level < -DailyFuelAllocator.DEFICIT_WARNING_THRESHOLD)
                violations.Add(new FuelViolation(date, FuelViolationKind.Deficit, Math.Round(level, 2)));
        }

        return violations;
    }

    public static double EndingLevel(
        Domain.Entities.Vehicle vehicle,
        Dictionary<DateTime, List<Domain.Entities.Trip>> tripsByDate,
        IEnumerable<VehicleFuelAllocation> allocations)
    {
        return vehicle.InitialFuelLevel
               + allocations.Sum(a => a.LitersAllocated)
               - tripsByDate.Values.SelectMany(t => t).Sum(vehicle.CalculateFuelConsumption);
    }
}
