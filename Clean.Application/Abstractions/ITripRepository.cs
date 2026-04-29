using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(int id);
    Task<Trip?> GetWithDetailsAsync(int id);
    Task<List<Trip>> GetByPeriodAsync(int periodId);
    Task<List<Trip>> GetByPeriodWithDetailsAsync(int periodId);
    Task AddAsync(Trip trip);
    void Update(Trip trip);
    void Remove(Trip trip);
    Task<List<Trip>> GetAllAsync();
    void Delete(Trip trip);
}