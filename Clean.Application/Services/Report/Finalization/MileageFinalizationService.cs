using Clean.Application.Dtos.ReportPeriod;
using Clean.Domain.Entities;

namespace Clean.Application.Services.Report.Finalization;

/// <summary>
/// Handles vehicle mileage finalization - calculating ending mileage
/// and updating vehicles for the next period.
/// </summary>
public class MileageFinalizationService
{
    /// <summary>
    /// Preview mileage finalization without saving changes
    /// </summary>
    public MileageFinalizationSummary PreviewMileageFinalization(
        Domain.Entities.ReportPeriod period,
        List<Domain.Entities.Vehicle> vehicles)
    {
        var summary = new MileageFinalizationSummary
        {
            VehicleUpdates = []
        };

        // Get all vehicles that had trips in this period
        var vehicleIdsWithTrips = period.Trips
            .SelectMany(t => t.Assignments)
            .Where(a => !a.HasConflict && a.VehicleId.HasValue)
            .Select(a => a.VehicleId!.Value)
            .Distinct()
            .ToHashSet();

        foreach (var vehicle in vehicles.Where(v => vehicleIdsWithTrips.Contains(v.Id)))
        {
            var update = CalculateVehicleMileageUpdate(vehicle, period);
            summary.VehicleUpdates.Add(update);
        }

        summary.VehiclesUpdated = summary.VehicleUpdates.Count;
        summary.TotalDistanceDriven = summary.VehicleUpdates.Sum(v => v.DistanceDriven);
        summary.VehiclesWithDiscrepancy = summary.VehicleUpdates.Count(v => v.HasDiscrepancy);

        return summary;
    }

    /// <summary>
    /// Finalize mileage and update vehicle current mileage values
    /// </summary>
    public MileageFinalizationSummary FinalizeMileage(
        Domain.Entities.ReportPeriod period,
        List<Domain.Entities.Vehicle> vehicles)
    {
        var summary = new MileageFinalizationSummary
        {
            VehicleUpdates = new List<VehicleMileageUpdateDto>()
        };

        // Get all vehicles that had trips in this period
        var vehicleIdsWithTrips = period.Trips
            .SelectMany(t => t.Assignments)
            .Where(a => !a.HasConflict && a.VehicleId.HasValue)
            .Select(a => a.VehicleId!.Value)
            .Distinct()
            .ToHashSet();

        foreach (var vehicle in vehicles.Where(v => vehicleIdsWithTrips.Contains(v.Id)))
        {
            var update = CalculateVehicleMileageUpdate(vehicle, period);
            summary.VehicleUpdates.Add(update);

            // UPDATE the vehicle's current mileage for next period
            vehicle.CurrentMileage = update.NewMileage;
            vehicle.MileageUpdatedAt = DateTime.UtcNow;
            vehicle.UpdatedAt = DateTime.UtcNow;

            Console.WriteLine($"  Vehicle {vehicle.PlateNumber}: " +
                            $"{update.StartingMileage:N0} km + {update.DistanceDriven:N1} km = " +
                            $"{update.NewMileage:N0} km");
        }

        summary.VehiclesUpdated = summary.VehicleUpdates.Count;
        summary.TotalDistanceDriven = summary.VehicleUpdates.Sum(v => v.DistanceDriven);
        summary.VehiclesWithDiscrepancy = summary.VehicleUpdates.Count(v => v.HasDiscrepancy);

        return summary;
    }

    private VehicleMileageUpdateDto CalculateVehicleMileageUpdate(
        Domain.Entities.Vehicle vehicle,
        Domain.Entities.ReportPeriod period)
    {
        // Get trips assigned to this vehicle
        var vehicleTrips = period.Trips
            .Where(t => t.Assignments.Any(a => a.VehicleId == vehicle.Id && !a.HasConflict))
            .ToList();

        // Calculate total distance driven
        double totalDistance = vehicleTrips.Sum(t => t.DistanceKm ?? 0);

        // Count trips with missing distance
        int tripsWithoutDistance = vehicleTrips.Count(t => !t.DistanceKm.HasValue || t.DistanceKm == 0);

        // Starting mileage (current value before finalization)
        double startingMileage = vehicle.CurrentMileage;

        // New mileage after this period
        double newMileage = startingMileage + totalDistance;

        return new VehicleMileageUpdateDto
        {
            VehicleId = vehicle.Id,
            PlateNumber = vehicle.PlateNumber,
            VehicleModel = vehicle.VehicleType?.Name ?? "Unknown",
            StartingMileage = startingMileage,
            DistanceDriven = totalDistance,
            NewMileage = newMileage,
            TripCount = vehicleTrips.Count,
            TripsWithoutDistance = tripsWithoutDistance,
            HasDiscrepancy = tripsWithoutDistance > 0
        };
    }
}

// ============================================================================
// DTOs for Mileage Finalization
// ============================================================================

/// <summary>
/// Summary of mileage finalization results
/// </summary>
public class MileageFinalizationSummary
{
    public int VehiclesUpdated { get; set; }
    public double TotalDistanceDriven { get; set; }
    public int VehiclesWithDiscrepancy { get; set; }
    public List<VehicleMileageUpdateDto> VehicleUpdates { get; set; } = new();
}

/// <summary>
/// Individual vehicle mileage update details
/// </summary>
public class VehicleMileageUpdateDto
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    
    /// <summary>
    /// Mileage at the start of the period
    /// </summary>
    public double StartingMileage { get; set; }
    
    /// <summary>
    /// Total distance driven during this period
    /// </summary>
    public double DistanceDriven { get; set; }
    
    /// <summary>
    /// New mileage after finalization (StartingMileage + DistanceDriven)
    /// </summary>
    public double NewMileage { get; set; }
    
    /// <summary>
    /// Number of trips this vehicle completed
    /// </summary>
    public int TripCount { get; set; }
    
    /// <summary>
    /// Number of trips without distance data
    /// </summary>
    public int TripsWithoutDistance { get; set; }
    
    /// <summary>
    /// True if some trips are missing distance data
    /// </summary>
    public bool HasDiscrepancy { get; set; }
}