using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.Trip;

namespace Clean.Application.Abstractions;

public interface ITripService
{
    Task<Response<List<TripDto>>> GetTripsByPeriodAsync(int periodId);
    Task<Response<TripDto>> GetTripByIdAsync(int id);
    Task<Response<string>> UpdateTripAsync(UpdateTripDto dto);
    Task<Response<double?>> RecalculateTripDistanceAsync(int id);
    Task<Response<string>> DeleteTripAsync(int id);
}