using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Mapbox;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.Trip;
using Clean.Application.Services.Report;
using Microsoft.EntityFrameworkCore;

namespace Clean.Application.Services.Trip;

public class TripService : ITripService
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

    public TripService(IUnitOfWork uow, IMapboxService mapboxService)
    {
        _uow = uow;
        _mapboxService = mapboxService;
    }

    public async Task<Response<List<TripDto>>> GetTripsByPeriodAsync(int periodId)
    {
        try
        {
            var trips = await _uow.Trips.GetByPeriodWithDetailsAsync(periodId);

            var dtos = trips.Select(MapToDto).ToList();

            return new Response<List<TripDto>>(HttpStatusCode.OK, dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<TripDto>>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<TripDto>> GetTripByIdAsync(int id)
    {
        try
        {
            var trip = await _uow.Trips.GetWithDetailsAsync(id);

            if (trip == null)
            {
                return new Response<TripDto>(HttpStatusCode.NotFound, "Trip not found");
            }

            return new Response<TripDto>(HttpStatusCode.OK, MapToDto(trip));
        }
        catch (Exception ex)
        {
            return new Response<TripDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<string>> UpdateTripAsync(UpdateTripDto dto)
    {
        try
        {
            var trip = await _uow.Trips.GetByIdAsync(dto.Id);

            if (trip == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, "Trip not found");
            }

            // Update only provided fields
            if (dto.ConfNumber != null)
                trip.ConfNumber = dto.ConfNumber;

            if (dto.PickUpDate.HasValue)
                trip.PickUpDate = DateTime.SpecifyKind(dto.PickUpDate.Value, DateTimeKind.Utc);

            if (!string.IsNullOrEmpty(dto.GarageOutTime))
                trip.GarageOutTime = TimeSpan.Parse(dto.GarageOutTime);

            if (!string.IsNullOrEmpty(dto.GarageInTime))
                trip.GarageInTime = TimeSpan.Parse(dto.GarageInTime);

            if (dto.CompanyName != null)
                trip.CompanyName = dto.CompanyName;

            if (dto.RoutingDetails != null)
                trip.RoutingDetails = dto.RoutingDetails;

            if (dto.DistanceKm.HasValue)
            {
                trip.DistanceKm = dto.DistanceKm.Value;
                trip.CoordinatesResolved = true;
            }

            if (dto.IncludedInReport.HasValue)
                trip.IncludedInReport = dto.IncludedInReport.Value;

            // These can be explicitly set to null
            trip.ImportedDriverName = dto.ImportedDriverName;
            trip.ImportedVehiclePlate = dto.ImportedVehiclePlate;
            trip.PmtMethod = dto.PmtMethod;

            if (dto.VehicleTypeId.HasValue)
                trip.VehicleTypeId = dto.VehicleTypeId.Value;

            if (dto.ServiceTypeId.HasValue)
                trip.ServiceTypeId = dto.ServiceTypeId.Value;

            _uow.Trips.Update(trip);
            await _uow.CompleteAsync();

            return new Response<string>(HttpStatusCode.OK, "Trip updated successfully", "Success");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<double?>> RecalculateTripDistanceAsync(int id)
    {
        try
        {
            var trip = await _uow.Trips.GetWithDetailsAsync(id);

            if (trip == null)
            {
                return new Response<double?>(HttpStatusCode.NotFound, "Trip not found");
            }

            var routingDetails = trip.RoutingDetails;

            if (string.IsNullOrWhiteSpace(routingDetails))
            {
                return new Response<double?>(HttpStatusCode.BadRequest,
                    "No routing details available for distance calculation");
            }

            var serviceTypeName = trip.ServiceType?.Name ?? "";
            var tripStart = trip.GetStartDateTime();
            var tripEnd = trip.GetEndDateTime();

            var distanceResult = await CalculateTripDistanceAsync(
                routingDetails, tripStart, tripEnd, serviceTypeName);

            if (distanceResult.StatusCode == 200)
            {
                trip.DistanceKm = distanceResult.Data;
                trip.CoordinatesResolved = true;
                _uow.Trips.Update(trip);
                await _uow.CompleteAsync();

                return new Response<double?>(HttpStatusCode.OK, distanceResult.Data);
            }
            else
            {
                trip.CoordinatesResolved = false;
                _uow.Trips.Update(trip);
                await _uow.CompleteAsync();

                return new Response<double?>(HttpStatusCode.BadRequest,
                    $"Failed to calculate distance: {string.Join(", ", distanceResult.Errors)}");
            }
        }
        catch (Exception ex)
        {
            return new Response<double?>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<string>> DeleteTripAsync(int id)
    {
        try
        {
            var trip = await _uow.Trips.GetByIdAsync(id);

            if (trip == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, "Trip not found");
            }

            // Delete any assignments associated with this trip
            var assignments = await _uow.Context.DriverAssignments
                .Where(a => a.TripId == id)
                .ToListAsync();

            if (assignments.Any())
            {
                _uow.Context.DriverAssignments.RemoveRange(assignments);
            }

            _uow.Trips.Remove(trip);
            await _uow.CompleteAsync();

            return new Response<string>(HttpStatusCode.OK, "Trip deleted successfully", "Success");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    private static TripDto MapToDto(Domain.Entities.Trip trip)
    {
        return new TripDto
        {
            Id = trip.Id,
            ConfNumber = trip.ConfNumber!,
            PickUpDate = trip.PickUpDate,
            GarageOutTime = trip.GarageOutTime.ToString(@"hh\:mm\:ss"),
            GarageInTime = trip.GarageInTime.ToString(@"hh\:mm\:ss"),
            CompanyName = trip.CompanyName,
            RoutingDetails = trip.RoutingDetails,
            DistanceKm = trip.DistanceKm,
            CoordinatesResolved = trip.CoordinatesResolved,
            IncludedInReport = trip.IncludedInReport,
            ImportedDriverName = trip.ImportedDriverName,
            ImportedVehiclePlate = trip.ImportedVehiclePlate,
            PmtMethod = trip.PmtMethod,
            VehicleTypeName = trip.VehicleType?.Name ?? "",
            ServiceTypeName = trip.ServiceType?.Name ?? "",
            VehicleTypeId = trip.VehicleTypeId,
            ServiceTypeId = trip.ServiceTypeId,
            ReportPeriodId = trip.ReportPeriodId
        };
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
                var result = await _mapboxService.CalculateDistanceAsync(
                    parsed.PickUp, parsed.DropOff, parsed.Stops ?? new List<LocationWithCoordinates>());

                if (result.StatusCode == 200)
                    apiDistance = result.Data;
            }
            else
            {
                var fullRouteStops = new List<LocationWithCoordinates> { parsed.PickUp };

                if (parsed.Stops != null)
                    fullRouteStops.AddRange(parsed.Stops);

                fullRouteStops.Add(parsed.DropOff);

                var result = await _mapboxService.CalculateDistanceAsync(
                    OFFICE_LOCATION, OFFICE_LOCATION, fullRouteStops);

                if (result.StatusCode == 200)
                    apiDistance = result.Data;
            }

            var hours = (tripEnd - tripStart).TotalHours;
            var durationEstimate = hours * 15.0;

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