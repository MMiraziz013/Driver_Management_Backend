using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class VehicleFuelAllocationRepository : IVehicleFuelAllocationRepository
{
    private readonly DataContext _context;

    public VehicleFuelAllocationRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<VehicleFuelAllocation?> GetByIdAsync(int id)
    {
        return await _context.VehicleFuelAllocations
            .Include(a => a.GasPurchase)
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<VehicleFuelAllocation>> GetAllAsync()
    {
        return await _context.VehicleFuelAllocations
            .Include(a => a.GasPurchase)
            .Include(a => a.Vehicle)
            .OrderBy(a => a.AllocationDate)
            .ToListAsync();
    }

    public async Task<List<VehicleFuelAllocation>> GetByPeriodIdAsync(int periodId)
    {
        return await _context.VehicleFuelAllocations
            .Where(a => a.ReportPeriodId == periodId)
            .Include(a => a.GasPurchase)
            .Include(a => a.Vehicle)
            .OrderBy(a => a.AllocationDate)
            .ToListAsync();
    }

    public async Task<List<VehicleFuelAllocation>> GetByVehicleIdAsync(int vehicleId)
    {
        return await _context.VehicleFuelAllocations
            .Where(a => a.VehicleId == vehicleId)
            .Include(a => a.GasPurchase)
            .OrderBy(a => a.AllocationDate)
            .ToListAsync();
    }

    public async Task<List<VehicleFuelAllocation>> GetByVehicleAndPeriodAsync(int vehicleId, int periodId)
    {
        return await _context.VehicleFuelAllocations
            .Where(a => a.VehicleId == vehicleId && a.ReportPeriodId == periodId)
            .Include(a => a.GasPurchase)
            .OrderBy(a => a.AllocationDate)
            .ToListAsync();
    }

    public async Task<List<VehicleFuelAllocation>> GetByPurchaseIdAsync(int purchaseId)
    {
        return await _context.VehicleFuelAllocations
            .Where(a => a.GasPurchaseId == purchaseId)
            .Include(a => a.Vehicle)
            .ToListAsync();
    }

    public async Task AddAsync(VehicleFuelAllocation entity)
    {
        await _context.VehicleFuelAllocations.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<VehicleFuelAllocation> entities)
    {
        await _context.VehicleFuelAllocations.AddRangeAsync(entities);
    }

    public void Remove(VehicleFuelAllocation entity)
    {
        _context.VehicleFuelAllocations.Remove(entity);
    }

    public void RemoveRange(IEnumerable<VehicleFuelAllocation> entities)
    {
        _context.VehicleFuelAllocations.RemoveRange(entities);
    }
}
