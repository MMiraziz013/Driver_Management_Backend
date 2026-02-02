using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class GasPurchaseRepository : IGasPurchaseRepository
{
    private readonly DataContext _context;

    public GasPurchaseRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<GasPurchase?> GetByIdAsync(int id)
    {
        return await _context.GasPurchases
            .Include(g => g.Allocations)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<List<GasPurchase>> GetAllAsync()
    {
        return await _context.GasPurchases
            .OrderBy(g => g.PurchaseDate)
            .ToListAsync();
    }

    public async Task<List<GasPurchase>> GetByPeriodIdAsync(int periodId)
    {
        return await _context.GasPurchases
            .Where(g => g.ReportPeriodId == periodId)
            .OrderBy(g => g.PurchaseDate)
            .ToListAsync();
    }

    public async Task<List<GasPurchase>> GetByPeriodAndFuelTypeAsync(int periodId, string fuelType)
    {
        return await _context.GasPurchases
            .Where(g => g.ReportPeriodId == periodId && g.FuelType == fuelType)
            .OrderBy(g => g.PurchaseDate)
            .ToListAsync();
    }

    public async Task AddAsync(GasPurchase entity)
    {
        await _context.GasPurchases.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<GasPurchase> entities)
    {
        await _context.GasPurchases.AddRangeAsync(entities);
    }

    public void Remove(GasPurchase entity)
    {
        _context.GasPurchases.Remove(entity);
    }

    public void RemoveRange(IEnumerable<GasPurchase> entities)
    {
        _context.GasPurchases.RemoveRange(entities);
    }
}
