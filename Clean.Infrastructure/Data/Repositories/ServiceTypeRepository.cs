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
}