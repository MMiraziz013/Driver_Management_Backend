using Clean.Application.Dtos.Fuel;
using Clean.Application.Dtos.ReportPeriod;
using Clean.Domain.Entities;

namespace Clean.Application.Services.Report.Finalization;

/// <summary>
/// Handles fuel allocation finalization - calculating ending fuel levels
/// and updating vehicles for the next period.
/// </summary>
public class FuelFinalizationService
{
    /// <summary>
    /// Preview fuel finalization without saving changes
    /// </summary>
    public FuelFinalizationSummary PreviewFuelFinalization(
        Domain.Entities.ReportPeriod period,
        List<Domain.Entities.Vehicle> vehicles,
        List<VehicleFuelAllocation> allocations)
    {
        var summary = new FuelFinalizationSummary   
        {
            VehicleUpdates = new List<VehicleFuelUpdateDto>()
        };

        foreach (var vehicle in GetFuelVehicles(vehicles))
        {
            var update = CalculateVehicleFuelUpdate(vehicle, period, allocations);
            summary.VehicleUpdates.Add(update);
        }

        summary.VehiclesUpdated = summary.VehicleUpdates.Count;
        summary.VehiclesWithDeficit = summary.VehicleUpdates.Count(v => v.HasDeficit);
        summary.TotalFuelAllocated = summary.VehicleUpdates.Sum(v => v.FuelAllocated);
        summary.TotalFuelConsumed = summary.VehicleUpdates.Sum(v => v.FuelConsumed);

        return summary;
    }

    /// <summary>
    /// Finalize fuel allocation and update vehicle initial levels
    /// </summary>
    public FuelFinalizationSummary FinalizeFuelAllocation(
        Domain.Entities.ReportPeriod period,
        List<Domain.Entities.Vehicle> vehicles,
        List<VehicleFuelAllocation> allocations)
    {
        var summary = new FuelFinalizationSummary
        {
            VehicleUpdates = new List<VehicleFuelUpdateDto>()
        };

        foreach (var vehicle in GetFuelVehicles(vehicles))
        {
            var update = CalculateVehicleFuelUpdate(vehicle, period, allocations);
            summary.VehicleUpdates.Add(update);

            // UPDATE the vehicle's initial fuel level for next period
            vehicle.InitialFuelLevel = update.NewInitialLevel;
            vehicle.UpdatedAt = DateTime.UtcNow;

            Console.WriteLine($"  Vehicle {vehicle.PlateNumber}: " +
                            $"{update.PreviousInitialLevel:F1}L + {update.FuelAllocated:F1}L - {update.FuelConsumed:F1}L = " +
                            $"{update.CalculatedFinalLevel:F1}L → New Initial: {update.NewInitialLevel:F1}L");
        }

        summary.VehiclesUpdated = summary.VehicleUpdates.Count;
        summary.VehiclesWithDeficit = summary.VehicleUpdates.Count(v => v.HasDeficit);
        summary.TotalFuelAllocated = summary.VehicleUpdates.Sum(v => v.FuelAllocated);
        summary.TotalFuelConsumed = summary.VehicleUpdates.Sum(v => v.FuelConsumed);

        return summary;
    }

    private VehicleFuelUpdateDto CalculateVehicleFuelUpdate(
        Domain.Entities.Vehicle vehicle,
        Domain.Entities.ReportPeriod period,
        List<VehicleFuelAllocation> allocations)
    {
        // Get trips assigned to this vehicle
        var vehicleTrips = period.Trips
            .Where(t => t.Assignments.Any(a => a.VehicleId == vehicle.Id && !a.HasConflict))
            .ToList();

        // Calculate fuel consumed based on distance
        double totalDistance = vehicleTrips.Sum(t => t.DistanceKm ?? 0);
        double fuelConsumed = vehicle.CalculateFuelConsumption(totalDistance);

        // Get total fuel allocated
        double fuelAllocated = allocations
            .Where(a => a.VehicleId == vehicle.Id)
            .Sum(a => a.LitersAllocated);

        // Calculate final fuel level
        double previousInitial = vehicle.InitialFuelLevel;
        double finalFuelLevel = previousInitial + fuelAllocated - fuelConsumed;

        // New initial for next period (clamped to valid range)
        double newInitialForNextPeriod = Math.Max(0, Math.Min(finalFuelLevel, vehicle.FuelTankCapacity));

        return new VehicleFuelUpdateDto
        {
            VehicleId = vehicle.Id,
            PlateNumber = vehicle.PlateNumber,
            FuelType = vehicle.FuelType ?? "",
            PreviousInitialLevel = previousInitial,
            FuelAllocated = fuelAllocated,
            FuelConsumed = fuelConsumed,
            CalculatedFinalLevel = finalFuelLevel,
            NewInitialLevel = newInitialForNextPeriod,
            HasDeficit = finalFuelLevel < 0
        };
    }

    private IEnumerable<Domain.Entities.Vehicle> GetFuelVehicles(List<Domain.Entities.Vehicle> vehicles)
    {
        return vehicles.Where(v =>
            !string.IsNullOrEmpty(v.FuelType) &&
            !v.FuelType.Equals("Electric", StringComparison.OrdinalIgnoreCase) &&
            !v.FuelType.Equals("Электро", StringComparison.OrdinalIgnoreCase));
    }
}