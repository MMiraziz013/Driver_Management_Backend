using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class VehicleUnavailablePeriodRepository : IVehicleUnavailablePeriodRepository
{
    private readonly IDataContext _context;

    public VehicleUnavailablePeriodRepository(IDataContext context)
    {
        _context = context;
    }

    public async Task<VehicleUnavailablePeriod?> GetByIdAsync(int id)
    {
        return await _context.VehicleUnavailablePeriods
            .Include(p => p.Vehicle)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<VehicleUnavailablePeriod>> GetByVehicleIdAsync(int vehicleId)
    {
        return await _context.VehicleUnavailablePeriods
            .Where(p => p.VehicleId == vehicleId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
    }

    public async Task<List<VehicleUnavailablePeriod>> GetAllAsync()
    {
        return await _context.VehicleUnavailablePeriods
            .Include(p => p.Vehicle)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
    }

    public async Task<List<VehicleUnavailablePeriod>> GetActivePeriodsAsync(DateTime date)
    {
        var dateOnly = date.Date;
        return await _context.VehicleUnavailablePeriods
            .Where(p => p.StartDate <= dateOnly && p.EndDate >= dateOnly)
            .ToListAsync();
    }

    public async Task<List<VehicleUnavailablePeriod>> GetOverlappingPeriodsAsync(
        int vehicleId, DateTime startDate, DateTime endDate, int? excludeId = null)
    {
        var query = _context.VehicleUnavailablePeriods
            .Where(p => p.VehicleId == vehicleId)
            .Where(p => p.StartDate <= endDate && p.EndDate >= startDate);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task AddAsync(VehicleUnavailablePeriod period)
    {
        await _context.VehicleUnavailablePeriods.AddAsync(period);
    }

    public void Update(VehicleUnavailablePeriod period)
    {
        _context.VehicleUnavailablePeriods.Update(period);
    }

    public void Delete(VehicleUnavailablePeriod period)
    {
        _context.VehicleUnavailablePeriods.Remove(period);
    }
}