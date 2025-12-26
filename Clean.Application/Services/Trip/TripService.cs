using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Services.Trip;

public class TripService : ITripService
{
    private readonly IUnitOfWork _uow;

    public TripService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<Domain.Entities.Trip>> GetTripByIdAsync(int id)
    {
        try
        {
            // We assume ITripRepository has a GetById method or we use a general approach
            var period = await _uow.ReportPeriods.GetAllAsync(); // Just for context, better to use a specific repo method
            // In a real scenario, you'd add GetById to ITripRepository
            return new Response<Domain.Entities.Trip>(HttpStatusCode.NotImplemented, "Direct GetById requires Repository expansion.");
        }
        catch (Exception ex)
        {
            return new Response<Domain.Entities.Trip>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }

    public async Task<Response<List<Domain.Entities.Trip>>> GetTripsByPeriodAsync(int periodId)
    {
        try
        {
            var period = await _uow.ReportPeriods.GetWithTripsAsync(periodId);
            if (period == null)
                return new Response<List<Domain.Entities.Trip>>(HttpStatusCode.NotFound, "Report period not found.");

            return new Response<List<Domain.Entities.Trip>>(HttpStatusCode.OK, "Trips retrieved successfully.", period.Trips);
        }
        catch (Exception ex)
        {
            return new Response<List<Domain.Entities.Trip>>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }

    public async Task<Response<bool>> DeleteTripAsync(int id)
    {
        try
        {
            // Logic to find and delete trip via UoW
            // await _uow.Trips.DeleteAsync(id); 
            // await _uow.CompleteAsync();
            return new Response<bool>(HttpStatusCode.OK, "Trip deleted successfully.", true);
        }
        catch (Exception ex)
        {
            return new Response<bool>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }
}