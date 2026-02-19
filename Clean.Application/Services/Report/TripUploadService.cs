using System.Globalization;
using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Mapbox;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;
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

    public async Task<Response<string>> UploadReportAsync(IFormFile file, int periodId)
    {
        try
        {
            await DeleteExistingTripsAsync(periodId);

            await using var stream = file.OpenReadStream();
            var rows = await stream.QueryAsync(useHeaderRow: true);

            var vTypes = await _uow.VehicleTypes.GetAllAsync();
            var sTypes = await _uow.ServiceTypes.GetAllAsync();

            int processedCount = 0;
            int distanceCalculatedCount = 0;
            int distanceFailedCount = 0;
            int noCoordinatesCount = 0;

            foreach (var row in rows)
            {
                string vName = row.VehicleType?.ToString() ?? "";
                string sName = row.ServiceType?.ToString() ?? "";
                string pmtMethod = row.PmtMethod?.ToString() ?? "";

                if (pmtMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"⏭️ Skipping cash payment trip: {row.ConfNumber}");
                    continue;
                }

                var vType = vTypes.FirstOrDefault(x => x.Name.Equals(vName, StringComparison.OrdinalIgnoreCase));
                var sType = sTypes.FirstOrDefault(x => x.Name.Equals(sName, StringComparison.OrdinalIgnoreCase));

                if (vType == null || sType == null) continue;

                DateTime parsedDate = DateTime.ParseExact(row.PickUpDate.ToString(), "dd/MM/yyyy",
                    CultureInfo.InvariantCulture);

                var trip = new Domain.Entities.Trip
                {
                    ReportPeriodId = periodId,
                    ConfNumber = row.ConfNumber?.ToString() ?? "",
                    PickUpDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc),
                    GarageOutTime = TimeSpan.Parse(row.GarageOutTime.ToString()),
                    GarageInTime = TimeSpan.Parse(row.GarageInTime.ToString()),
                    CompanyName = row.CompanyName?.ToString() ?? "Unknown",
                    RoutingDetails = row.RoutingDetails?.ToString() ?? "N/A",
                    VehicleTypeId = vType.Id,
                    ServiceTypeId = sType.Id,
                    IncludedInReport = true,
                    ImportedDriverName = row.Driver?.ToString(),
                    ImportedVehiclePlate = row.Car?.ToString(),
                    PmtMethod = row.PmtMethod
                };

                // Calculate distance
                var routingDetails = row.RoutingDetails?.ToString() ?? "";
                bool coordinatesFound = true;

                if (!string.IsNullOrWhiteSpace(routingDetails))
                {
                    try
                    {
                        var distanceResult = await CalculateTripDistanceAsync(
                            routingDetails,
                            trip.GetStartDateTime(),
                            trip.GetEndDateTime(),
                            sType.Name);

                        if (distanceResult.StatusCode == 200)
                        {
                            trip.DistanceKm = distanceResult.Data;
                            distanceCalculatedCount++;
                        }
                        else
                        {
                            distanceFailedCount++;
                            coordinatesFound = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        distanceFailedCount++;
                        coordinatesFound = false;
                        Console.WriteLine($"Error calculating distance: {ex.Message}");
                    }
                }
                else
                {
                    coordinatesFound = false;
                    noCoordinatesCount++;
                }

                trip.CoordinatesResolved = coordinatesFound;
                await _uow.Trips.AddAsync(trip);
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
            {
                _uow.Context.DriverAssignments.RemoveRange(existingAssignments);
            }

            _uow.Context.Trips.RemoveRange(existingTrips);
            await _uow.CompleteAsync();
        }
    }

    private async Task<Response<double>> CalculateTripDistanceAsync(
        string routingDetails,
        DateTime tripStart,
        DateTime tripEnd,
        string serviceTypeName)
    {
        try
        {
            var parsed = RoutingDetailsParser.Parse(routingDetails);

            if (parsed.PickUp == null || string.IsNullOrWhiteSpace(parsed.PickUp.Address) ||
                parsed.DropOff == null || string.IsNullOrWhiteSpace(parsed.DropOff.Address))
            {
                return new Response<double>(HttpStatusCode.BadRequest, "Missing pickup or dropoff");
            }

            bool isFieldTrip = serviceTypeName.Equals("Field Trip", StringComparison.OrdinalIgnoreCase);
            bool isRoundTrip = serviceTypeName.Equals("Round Trip", StringComparison.OrdinalIgnoreCase);
            bool isCustomItinerary = serviceTypeName.Equals("Custom Itinerary", StringComparison.OrdinalIgnoreCase);

            double apiDistance = 0;

            if (isFieldTrip)
            {
                // Field Trip: route as-is (no office routing)
                var result = await _mapboxService.CalculateDistanceAsync(
                    parsed.PickUp, parsed.DropOff, parsed.Stops ?? new List<LocationWithCoordinates>());
                
                if (result.StatusCode == 200)
                    apiDistance = result.Data;
            }
            else
            {
                // Regular trips: Office → PickUp → Stops → DropOff → Office
                var fullRouteStops = new List<LocationWithCoordinates> { parsed.PickUp };
                
                if (parsed.Stops != null)
                    fullRouteStops.AddRange(parsed.Stops);
                
                fullRouteStops.Add(parsed.DropOff);

                var result = await _mapboxService.CalculateDistanceAsync(
                    OFFICE_LOCATION, OFFICE_LOCATION, fullRouteStops);

                if (result.StatusCode == 200)
                    apiDistance = result.Data;
            }

            // Duration-based fallback
            var hours = (tripEnd - tripStart).TotalHours;
            var durationEstimate = hours * 10.0;

            double finalDistance;

            if (isRoundTrip)
            {
                finalDistance = apiDistance > 0 ? apiDistance : durationEstimate;
            }
            else if (isCustomItinerary)
            {
                finalDistance = Math.Max(apiDistance, durationEstimate);
            }
            else
            {
                finalDistance = apiDistance > 0 ? apiDistance : durationEstimate;
            }

            return new Response<double>(HttpStatusCode.OK, Math.Round(finalDistance, 2));
        }
        catch (Exception ex)
        {
            return new Response<double>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }
}