using Clean.Application.Dtos.Report;
using Clean.Domain.Entities;

namespace Clean.Application.Services.Report;

/// <summary>
/// Service for grouping trips into journeys based on business rules:
/// - Same driver and vehicle
/// - Break between trips ≤ 4 hours
/// - Vehicle not used for other trips during the break
/// </summary>
public class JourneyGroupingService
{
    private const int MAX_BREAK_HOURS = 10;

    /// <summary>
    /// Group trips into journeys for a given period
    /// </summary>
    public List<JourneyDto> GroupTripsIntoJourneys(
        Domain.Entities.ReportPeriod period,
        Dictionary<int, double> vehicleStartingMileages)
    {
        var journeys = new List<JourneyDto>();

        // Get all successfully assigned trips (not conflicts)
        var assignedTrips = period.Trips
            .Where(t => t.Assignments.Any(a => !a.HasConflict && a.DriverId.HasValue && a.VehicleId.HasValue))
            .OrderBy(t => t.PickUpDate)
            .ThenBy(t => t.GarageOutTime)
            .ToList();

        if (!assignedTrips.Any())
            return journeys;

        // Group by date first
        var tripsByDate = assignedTrips
            .GroupBy(t => t.PickUpDate.Date)
            .OrderBy(g => g.Key);

        int journeyNumber = 1;

        foreach (var dateGroup in tripsByDate)
        {
            var date = dateGroup.Key;
            var tripsOnDate = dateGroup.ToList();

            // Track which trips have been assigned to a journey
            var assignedTripIds = new HashSet<int>();

            // Group by driver-vehicle combination
            var driverVehicleGroups = tripsOnDate
                .Select(t => new
                {
                    Trip = t,
                    Assignment = t.Assignments.First(a => !a.HasConflict),
                    DriverId = t.Assignments.First(a => !a.HasConflict).DriverId!.Value,
                    VehicleId = t.Assignments.First(a => !a.HasConflict).VehicleId!.Value
                })
                .GroupBy(x => new { x.DriverId, x.VehicleId })
                .ToList();

            foreach (var dvGroup in driverVehicleGroups)
            {
                var driverId = dvGroup.Key.DriverId;
                var vehicleId = dvGroup.Key.VehicleId;

                // Get all trips for this driver-vehicle combo, sorted by time
                var dvTrips = dvGroup
                    .OrderBy(x => x.Trip.GarageOutTime)
                    .ToList();

                // Build journeys by checking breaks
                var currentJourneyTrips = new List<Domain.Entities.Trip>();

                foreach (var item in dvTrips)
                {
                    var trip = item.Trip;

                    if (assignedTripIds.Contains(trip.Id))
                        continue;

                    if (!currentJourneyTrips.Any())
                    {
                        // Start new journey
                        currentJourneyTrips.Add(trip);
                        assignedTripIds.Add(trip.Id);
                    }
                    else
                    {
                        // Check if this trip can be added to current journey
                        var lastTrip = currentJourneyTrips.Last();
                        var lastTripEnd = GetTripEndDateTime(lastTrip);
                        var thisTripStart = GetTripStartDateTime(trip);

                        var breakHours = (thisTripStart - lastTripEnd).TotalHours;

                        // Check break duration
                        if (breakHours <= MAX_BREAK_HOURS && breakHours >= 0)
                        {
                            // Check if vehicle was used by another driver during the break
                            var vehicleUsedDuringBreak = tripsOnDate
                                .Where(t => t.Id != trip.Id && !currentJourneyTrips.Select(ct => ct.Id).Contains(t.Id))
                                .Where(t => t.Assignments.Any(a => !a.HasConflict && a.VehicleId == vehicleId))
                                .Any(t =>
                                {
                                    var otherStart = GetTripStartDateTime(t);
                                    var otherEnd = GetTripEndDateTime(t);
                                    // Check if other trip overlaps with the break period
                                    return otherStart < thisTripStart && otherEnd > lastTripEnd;
                                });

                            if (!vehicleUsedDuringBreak)
                            {
                                // Add to the current journey
                                currentJourneyTrips.Add(trip);
                                assignedTripIds.Add(trip.Id);
                                continue;
                            }
                        }

                        // Break is too long or vehicle was used - finalize the current journey and start new one
                        if (currentJourneyTrips.Any())
                        {
                            var journey = CreateJourney(
                                journeyNumber++,
                                date,
                                currentJourneyTrips,
                                vehicleStartingMileages.GetValueOrDefault(vehicleId, 0));
                            journeys.Add(journey);

                            // Update starting mileage for next journey
                            vehicleStartingMileages[vehicleId] = journey.EndingMileage;
                        }

                        // Start a new journey with this trip
                        currentJourneyTrips = [trip];
                        assignedTripIds.Add(trip.Id);
                    }
                }

                // Finalize last journey for this driver-vehicle combo
                if (currentJourneyTrips.Any())
                {
                    var journey = CreateJourney(
                        journeyNumber++,
                        date,
                        currentJourneyTrips,
                        vehicleStartingMileages.GetValueOrDefault(vehicleId, 0));
                    journeys.Add(journey);

                    // Update starting mileage for potential future journeys
                    vehicleStartingMileages[vehicleId] = journey.EndingMileage;
                }
            }
        }

        return journeys.OrderBy(j => j.Date).ThenBy(j => j.DepartureTime).ToList();
    }

    /// <summary>
    /// Create a journey DTO from a group of trips
    /// </summary>
    private JourneyDto CreateJourney(
        int journeyNumber,
        DateTime date,
        List<Domain.Entities.Trip> trips,
        double startingMileage)
    {
        var firstTrip = trips.OrderBy(t => t.GarageOutTime).First();
        var lastTrip = trips.OrderBy(t => t.GarageInTime).Last();
        var assignment = firstTrip.Assignments.First(a => !a.HasConflict);

        // Calculate total distance
        double totalDistance = trips.Sum(t => t.DistanceKm ?? 0);

        // Calculate fuel consumption (using vehicle's consumption rate)
        double fuelConsumed = 0;
        if (assignment.Vehicle != null)
        {
            fuelConsumed = assignment.Vehicle.CalculateFuelConsumption(totalDistance);
        }

        // Get unique companies
        var companies = trips
            .Select(t => t.CompanyName)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();

        return new JourneyDto
        {
            JourneyNumber = journeyNumber,
            Date = date,
            DriverId = assignment.DriverId!.Value,
            DriverName = assignment.Driver?.FullName ?? "Unknown",
            VehicleId = assignment.VehicleId!.Value,
            VehiclePlate = assignment.Vehicle?.PlateNumber ?? "Unknown",
            VehicleModel = assignment.Vehicle?.VehicleType?.Name ?? "Unknown",
            DepartureTime = firstTrip.GarageOutTime,
            ReturnTime = lastTrip.GarageInTime,
            Companies = string.Join(", ", companies),
            ConfNumbers = trips.Select(t => t.ConfNumber).ToList(),
            StartingMileage = startingMileage,
            EndingMileage = startingMileage + totalDistance,
            TotalDistanceKm = totalDistance,
            TotalFuelConsumed = fuelConsumed,
            TripCount = trips.Count,
            Trips = trips.Select(t => new JourneyTripDto
            {
                TripId = t.Id,
                ConfNumber = t.ConfNumber!,
                GarageOutTime = t.GarageOutTime,
                GarageInTime = t.GarageInTime,
                CompanyName = t.CompanyName,
                RoutingDetails = t.RoutingDetails,
                DistanceKm = t.DistanceKm,
                ServiceType = t.ServiceType?.Name ?? "N/A"
            }).ToList()
        };
    }

    private DateTime GetTripStartDateTime(Domain.Entities.Trip trip)
    {
        return trip.PickUpDate.Date + trip.GarageOutTime;
    }

    private DateTime GetTripEndDateTime(Domain.Entities.Trip trip)
    {
        var endTime = trip.PickUpDate.Date + trip.GarageInTime;

        // Handle overnight trips
        if (trip.GarageInTime < trip.GarageOutTime)
        {
            endTime = endTime.AddDays(1);
        }

        return endTime;
    }
}
