using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class VehicleTypeRepository : IVehicleTypeRepository
{
    private readonly DataContext _context;

    public VehicleTypeRepository(DataContext context)
    {
        _context = context;
    }

    public IQueryable<VehicleType> Query()
    {
        return _context.VehicleTypes.AsNoTracking();
    }

    public async Task<List<VehicleType>> GetAllAsync()
    {
        return await _context.VehicleTypes.ToListAsync();
    }
    
    public async Task<VehicleType?> GetByIdAsync(int id)
    {
        return await _context.VehicleTypes.FindAsync(id);
    }

    public async Task AddAsync(VehicleType vehicleType)
    {
        await _context.VehicleTypes.AddAsync(vehicleType);
    }
}