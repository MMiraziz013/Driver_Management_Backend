using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IGasPurchaseRepository
{
    Task<GasPurchase?> GetByIdAsync(int id);
    Task<List<GasPurchase>> GetAllAsync();
    Task<List<GasPurchase>> GetByPeriodIdAsync(int periodId);
    Task<List<GasPurchase>> GetByPeriodAndFuelTypeAsync(int periodId, string fuelType);
    Task AddAsync(GasPurchase entity);
    Task AddRangeAsync(IEnumerable<GasPurchase> entities);
    void Remove(GasPurchase entity);
    void RemoveRange(IEnumerable<GasPurchase> entities);
}
