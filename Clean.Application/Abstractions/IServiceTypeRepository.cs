using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IServiceTypeRepository
{
    Task<List<ServiceType>> GetAllAsync();
    Task<ServiceType?> GetByIdAsync(int id);
    Task AddAsync(ServiceType serviceType);
    Task<ServiceType?> UpdateAsync(ServiceType toUpdate);
    Task<bool> DeleteAsync(int id);
}