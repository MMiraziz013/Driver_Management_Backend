using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IBonusSettingsRepository
{
    Task<BonusSettings?> GetActiveAsync();
    Task<BonusSettings?> GetByIdAsync(int id);
    Task AddAsync(BonusSettings settings);
    void Update(BonusSettings settings);
}