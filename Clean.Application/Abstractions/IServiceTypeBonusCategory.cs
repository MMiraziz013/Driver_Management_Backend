using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IServiceTypeBonusConfigRepository
{
    Task<List<ServiceTypeBonusConfig>> GetAllWithServiceTypeAsync();
    Task<ServiceTypeBonusConfig?> GetByServiceTypeIdAsync(int serviceTypeId);
    Task AddAsync(ServiceTypeBonusConfig config);
    void Update(ServiceTypeBonusConfig config);
    void Remove(ServiceTypeBonusConfig config);
    Task AddRangeAsync(IEnumerable<ServiceTypeBonusConfig> configs);
}