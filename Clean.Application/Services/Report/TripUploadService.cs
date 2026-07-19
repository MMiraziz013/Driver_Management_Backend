using System.Globalization;
using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Mapbox;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;

namespace Clean.Application.Services.Report;

/// <summary>
/// Handles trip upload from Excel files and distance calculation.
/// </summary>
public class TripUploadService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapboxService _mapboxService;

    private const double OFFICE_LATITUDE = 41.304388;
    private const double OFFICE_LONGITUDE = 69.282918;

    private static readonly LocationWithCoordinates OFFICE_LOCATION = new()
    {
        Address = "Office Parking",
        Latitude = OFFICE_LATITUDE,
        Longitude = OFFICE_LONGITUDE
    };

    public TripUploadService(IUnitOfWork uow, IMapboxService mapboxService)
    {
        _uow = uow;
        _mapboxService = mapboxService;
    }

    // Describes the routing status of a parsed row so Phase C can apply
    // EXACTLY the same counting your original loop did.
    private enum RoutingState
    {
        Empty,    // routing cell was blank          -> noCoordinatesCount
        Invalid,  // present but unparseable/no PU/DO -> distanceFailedCount
        Valid     // ready for a distance calculation
    }

    // Carries everything Phase C needs without re-parsing or re-geocoding.
    private sealed class PendingTrip
    {
        public required Domain.Entities.Trip Trip { get; init; }
        public required string ServiceTypeName { get; init; }
        public RoutingState State { get; init; }
        public LocationWithCoordinates? PickUp { get; init; }
        public LocationWithCoordinates? DropOff { get; init; }
        public List<LocationWithCoordinates> Stops { get; init; } = new();
    }

    public async Task<Response<string>> UploadReportAsync(IFormFile file, int periodId)
    {
        try
        {
            await DeleteExistingTripsAsync(periodId);

            await using var stream = file.OpenReadStream();
            var rows = await stream.QueryAsync(useHeaderRow: false);
            var rowsList = rows.ToList();

            var headerRow = rowsList.ElementAtOrDefault(6);
            if (headerRow == null)
                return new Response<string>(HttpStatusCode.BadRequest, "Could not find headers at row 7");

            var vTypes = await _uow.VehicleTypes.GetAllAsync();
            var sTypes = await _uow.ServiceTypes.GetAllAsync();
            var drivers = await _uow.Drivers.GetActiveDriversWithDetailsAsync();

            // ============================================================
            //  PHASE A — parse rows, build trips, extract locations.
            //            NO Mapbox calls here.
            // ============================================================
            var pending = new List<PendingTrip>();

            foreach (var row in rowsList.Skip(7))
            {
                var dict = (IDictionary<string, object>)row;
                var values = dict.Values.ToArray();

                string GetVal(int index) =>
                    index < values.Length ? values[index]?.ToString() ?? "" : "";

                var confNumber = GetVal(0);
                var pickUpDateStr = GetVal(2);
                var garageOut = GetVal(5);
                var garageIn = GetVal(6);
                var company = GetVal(21);
                var routing = GetVal(23);
                var driver = GetVal(25);
                var car = GetVal(26);
                var vName = GetVal(27);
                var sName = GetVal(28);
                var pmtMethod = GetVal(32);

                var vType = vTypes.FirstOrDefault(x => x.Name.Equals(vName, StringComparison.OrdinalIgnoreCase));
                var sType = sTypes.FirstOrDefault(x => x.Name.Equals(sName, StringComparison.OrdinalIgnoreCase));

                if (vType == null || sType == null) continue;

                var vehicleTypeId = vType.Id;

                if (!DateTime.TryParseExact(pickUpDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime parsedDate))
                    continue;

                vehicleTypeId = vehicleTypeId switch
                {
                    6 => 1,
                    3 or 4 => 2,
                    _ => vehicleTypeId
                };

                if (sName.Equals("Late Cancellation", StringComparison.OrdinalIgnoreCase))
                {
                    
                }

                
                var isSamarkandDriver = false;

                var tripDriver = drivers.FirstOrDefault(d=> d.FullName.Equals(driver, StringComparison.OrdinalIgnoreCase));

                if (tripDriver != null && tripDriver.EmploymentType == EmploymentType.Samarkand)
                {
                    isSamarkandDriver = true;
                }

                var trip = new Domain.Entities.Trip
                {
                    ReportPeriodId = periodId,
                    ConfNumber = confNumber,
                    PickUpDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc),
                    GarageOutTime = TimeSpan.TryParse(garageOut, out var go) ? go : TimeSpan.Zero,
                    GarageInTime = TimeSpan.TryParse(garageIn, out var gi) ? gi : TimeSpan.Zero,
                    CompanyName = string.IsNullOrWhiteSpace(company) ? "Unknown" : company,
                    RoutingDetails = string.IsNullOrWhiteSpace(routing) ? "N/A" : routing,
                    VehicleTypeId = vehicleTypeId,
                    ServiceTypeId = sType.Id,
                    IncludedInReport = true,
                    IsSamarkandTrip = isSamarkandDriver,
                    ImportedDriverName = driver,
                    ImportedVehiclePlate = car,
                    PmtMethod = pmtMethod
                };

                // Parse routing ONCE here (cheap, no I/O) and classify it.
                var state = RoutingState.Empty;
                LocationWithCoordinates? pickUp = null;
                LocationWithCoordinates? dropOff = null;
                var stops = new List<LocationWithCoordinates>();

                if (!string.IsNullOrWhiteSpace(routing))
                {
                    state = RoutingState.Invalid;
                    try
                    {
                        var parsed = RoutingDetailsParser.Parse(routing);
                        if (parsed.PickUp != null && !string.IsNullOrWhiteSpace(parsed.PickUp.Address) &&
                            parsed.DropOff != null && !string.IsNullOrWhiteSpace(parsed.DropOff.Address))
                        {
                            pickUp = parsed.PickUp;
                            dropOff = parsed.DropOff;
                            if (parsed.Stops != null) stops.AddRange(parsed.Stops);
                            state = RoutingState.Valid;
                        }
                    }
                    catch
                    {
                        state = RoutingState.Invalid;
                    }
                }

                pending.Add(new PendingTrip
                {
                    Trip = trip,
                    ServiceTypeName = sType.Name,
                    State = state,
                    PickUp = pickUp,
                    DropOff = dropOff,
                    Stops = stops
                });
            }

            // ============================================================
            //  PHASE B — resolve EVERY address across the whole sheet, once.
            //            Office has explicit coords, so it's skipped here.
            // ============================================================
            var allAddresses = pending
                .Where(p => p.State == RoutingState.Valid)
                .SelectMany(p => p.Stops.Prepend(p.DropOff!).Prepend(p.PickUp!))
                .Where(loc => loc is { Latitude: null } && !string.IsNullOrWhiteSpace(loc.Address))
                .Select(loc => loc.Address);

            var coordinateMap = await _mapboxService.ResolveAddressesAsync(allAddresses);

            // ============================================================
            //  PHASE C — compute distances from the in-memory map. No geocoding.
            // ============================================================
            var processedCount = 0;
            var distanceCalculatedCount = 0;
            var distanceFailedCount = 0;
            var noCoordinatesCount = 0;

            foreach (var p in pending)
            {
                bool coordinatesFound = true;

                switch (p.State)
                {
                    case RoutingState.Empty:
                        coordinatesFound = false;
                        noCoordinatesCount++;
                        break;

                    case RoutingState.Invalid:
                        coordinatesFound = false;
                        distanceFailedCount++;
                        break;

                    case RoutingState.Valid:
                        try
                        {
                            var distanceResult = await CalculateTripDistanceAsync(
                                p.PickUp!, p.DropOff!, p.Stops,
                                p.Trip.GetStartDateTime(), p.Trip.GetEndDateTime(),
                                p.ServiceTypeName, coordinateMap);

                            if (distanceResult.StatusCode == 200)
                            {
                                p.Trip.DistanceKm = distanceResult.Data;
                                distanceCalculatedCount++;
                            }
                            else
                            {
                                distanceFailedCount++;
                                coordinatesFound = false;
                            }
                        }
                        catch
                        {
                            distanceFailedCount++;
                            coordinatesFound = false;
                        }
                        break;
                }

                p.Trip.CoordinatesResolved = coordinatesFound;
                await _uow.Trips.AddAsync(p.Trip);
                processedCount++;
            }

            await _uow.CompleteAsync();

            var message = $"Processed {processedCount} trips. Distance calculated: {distanceCalculatedCount}, " +
                          $"Failed: {distanceFailedCount}, No coordinates: {noCoordinatesCount}";
            return new Response<string>(HttpStatusCode.OK, message, "Success");
        }
        catch (Exception ex)
        {
            var errorMsg = ex.InnerException?.Message ?? ex.Message;
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { errorMsg, ex.StackTrace ?? "" });
        }
    }

    private async Task DeleteExistingTripsAsync(int periodId)
    {
        var existingTrips = await _uow.Context.Trips
            .Where(t => t.ReportPeriodId == periodId)
            .ToListAsync();

        if (existingTrips.Any())
        {
            var tripIds = existingTrips.Select(t => t.Id).ToList();
            var existingAssignments = await _uow.Context.DriverAssignments
                .Where(a => tripIds.Contains(a.TripId))
                .ToListAsync();

            if (existingAssignments.Any())
                _uow.Context.DriverAssignments.RemoveRange(existingAssignments);

            _uow.Context.Trips.RemoveRange(existingTrips);
            await _uow.CompleteAsync();
        }
    }

    /// <summary>
    /// Distance for one trip. Parsing/geocoding already done — this only routes
    /// pre-resolved coordinates through the Directions API via coordinateMap.
    /// </summary>
    private async Task<Response<double>> CalculateTripDistanceAsync(
        LocationWithCoordinates pickUp,
        LocationWithCoordinates dropOff,
        List<LocationWithCoordinates> stops,
        DateTime tripStart,
        DateTime tripEnd,
        string serviceTypeName,
        IReadOnlyDictionary<string, (double Lat, double Lon)> coordinateMap)
    {
        try
        {
            bool isFieldTrip = serviceTypeName.Equals("Field Trip", StringComparison.OrdinalIgnoreCase);
            bool isRoundTrip = serviceTypeName.Equals("Round Trip", StringComparison.OrdinalIgnoreCase);
            bool isCustomItinerary = serviceTypeName.Equals("Custom Itinerary", StringComparison.OrdinalIgnoreCase);

            double apiDistance = 0;

            if (isFieldTrip)
            {
                // Field Trip: route as-is (no office routing)
                var result = await _mapboxService.CalculateDistanceAsync(
                    pickUp, dropOff, stops, coordinateMap);

                if (result.StatusCode == 200)
                    apiDistance = result.Data;
            }
            else
            {
                // Regular trips: Office -> PickUp -> Stops -> DropOff -> Office
                var fullRouteStops = new List<LocationWithCoordinates> { pickUp };
                fullRouteStops.AddRange(stops);
                fullRouteStops.Add(dropOff);

                var result = await _mapboxService.CalculateDistanceAsync(
                    OFFICE_LOCATION, OFFICE_LOCATION, fullRouteStops, coordinateMap);

                if (result.StatusCode == 200)
                    apiDistance = result.Data;
            }

            // Duration-based fallback
            var hours = (tripEnd - tripStart).TotalHours;
            var durationEstimate = hours * 15.0;

            double finalDistance;

            if (isRoundTrip)
                finalDistance = apiDistance > 0 ? apiDistance : durationEstimate;
            else if (isCustomItinerary)
                finalDistance = Math.Max(apiDistance, durationEstimate);
            else
                finalDistance = apiDistance > 0 ? apiDistance : durationEstimate;

            return new Response<double>(HttpStatusCode.OK, Math.Round(finalDistance, 2));
        }
        catch (Exception ex)
        {
            return new Response<double>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }
}