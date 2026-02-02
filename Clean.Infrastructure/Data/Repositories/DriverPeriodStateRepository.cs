using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class DriverPeriodStateRepository : IDriverPeriodStateRepository
{
    private readonly DataContext _context;

    public DriverPeriodStateRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<DriverPeriodState?> GetByDriverAndPeriodAsync(int driverId, int periodId)
    {
        return await _context.DriverPeriodStates
            .Include(s => s.Driver)
            .Include(s => s.ReportPeriod)
            .FirstOrDefaultAsync(s => s.DriverId == driverId && s.ReportPeriodId == periodId);
    }

    public async Task<List<DriverPeriodState>> GetByPeriodIdAsync(int periodId)
    {
        return await _context.DriverPeriodStates
            .Where(s => s.ReportPeriodId == periodId)
            .Include(s => s.Driver)
            .ToListAsync();
    }

    public async Task<DriverPeriodState?> GetLatestForDriverAsync(int driverId)
    {
        return await _context.DriverPeriodStates
            .Where(s => s.DriverId == driverId)
            .Include(s => s.ReportPeriod)
            .OrderByDescending(s => s.ReportPeriod.EndDate)
            .FirstOrDefaultAsync();
    }

    public async Task<List<DriverPeriodState>> GetByDriverIdsAndPeriodAsync(IEnumerable<int> driverIds, int periodId)
    {
        var driverIdList = driverIds.ToList();
        return await _context.DriverPeriodStates
            .Where(s => driverIdList.Contains(s.DriverId) && s.ReportPeriodId == periodId)
            .Include(s => s.Driver)
            .ToListAsync();
    }

    public async Task<bool> ExistsForPeriodAsync(int periodId)
    {
        return await _context.DriverPeriodStates
            .AnyAsync(s => s.ReportPeriodId == periodId);
    }

    public async Task AddAsync(DriverPeriodState state)
    {
        await _context.DriverPeriodStates.AddAsync(state);
    }

    public async Task AddRangeAsync(IEnumerable<DriverPeriodState> states)
    {
        await _context.DriverPeriodStates.AddRangeAsync(states);
    }

    public async Task DeleteByPeriodIdAsync(int periodId)
    {
        var states = await _context.DriverPeriodStates
            .Where(s => s.ReportPeriodId == periodId)
            .ToListAsync();
        
        _context.DriverPeriodStates.RemoveRange(states);
    }

    public async Task<List<DriverPeriodState>> GetDriverHistoryAsync(int driverId)
    {
        return await _context.DriverPeriodStates
            .Where(s => s.DriverId == driverId)
            .Include(s => s.ReportPeriod)
            .OrderByDescending(s => s.ReportPeriod.EndDate)
            .ToListAsync();
    }
}

