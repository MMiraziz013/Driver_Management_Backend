using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class ServiceTypeRepository : IServiceTypeRepository
{
    private readonly DataContext _context;

    public ServiceTypeRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceType>> GetAllAsync() => await _context.ServiceTypes.ToListAsync();

    public async Task<ServiceType?> GetByIdAsync(int id) => await _context.ServiceTypes.FindAsync(id);

    public async Task AddAsync(ServiceType serviceType) => await _context.ServiceTypes.AddAsync(serviceType);
    public async Task<ServiceType?> UpdateAsync(ServiceType toUpdate)
    {
        var exists = await _context.ServiceTypes.FindAsync(toUpdate.Id);
        if (exists is null)
        {
            return null;
        }

        exists.Name = toUpdate.Name;
        exists.Description = toUpdate.Description;

        var result = await _context.SaveChangesAsync();

        return result == 0 ? null : exists;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var exists = await _context.ServiceTypes.FindAsync(id);
        if (exists is null)
        {
            return false;
        }

        _context.ServiceTypes.Remove(exists);
        var result = await _context.SaveChangesAsync();

        return result > 0;
    }
}