using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface ITripService
{
    Task<Response<Trip>> GetTripByIdAsync(int id);
    Task<Response<List<Trip>>> GetTripsByPeriodAsync(int periodId);
    Task<Response<bool>> DeleteTripAsync(int id);
}