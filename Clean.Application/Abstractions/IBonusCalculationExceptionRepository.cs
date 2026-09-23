using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IBonusCalculationExceptionRepository
{
    Task<List<BonusCalculationException>> GetAllWithServiceTypeAsync();
    Task<List<BonusCalculationException>> GetActiveAsync();
    Task<BonusCalculationException?> GetByIdAsync(int id);
    Task AddAsync(BonusCalculationException exception);
    void Update(BonusCalculationException exception);
    void Remove(BonusCalculationException exception);
}
