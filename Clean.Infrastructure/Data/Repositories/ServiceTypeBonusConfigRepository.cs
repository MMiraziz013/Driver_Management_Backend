using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class ServiceTypeBonusConfigRepository : IServiceTypeBonusConfigRepository
{
    private readonly DataContext _context;

    public ServiceTypeBonusConfigRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceTypeBonusConfig>> GetAllWithServiceTypeAsync()
    {
        return await _context.ServiceTypeBonusConfigs
            .Include(c => c.ServiceType)
            .OrderBy(c => c.ServiceType.Name)
            .ToListAsync();
    }

    public async Task<ServiceTypeBonusConfig?> GetByServiceTypeIdAsync(int serviceTypeId)
    {
        return await _context.ServiceTypeBonusConfigs
            .Include(c => c.ServiceType)
            .FirstOrDefaultAsync(c => c.ServiceTypeId == serviceTypeId);
    }

    public async Task AddAsync(ServiceTypeBonusConfig config)
    {
        await _context.ServiceTypeBonusConfigs.AddAsync(config);
    }

    public void Update(ServiceTypeBonusConfig config)
    {
        _context.ServiceTypeBonusConfigs.Update(config);
    }

    public void Remove(ServiceTypeBonusConfig config)
    {
        _context.ServiceTypeBonusConfigs.Remove(config);
    }

    public async Task AddRangeAsync(IEnumerable<ServiceTypeBonusConfig> configs)
    {
        await _context.ServiceTypeBonusConfigs.AddRangeAsync(configs);
    }
}