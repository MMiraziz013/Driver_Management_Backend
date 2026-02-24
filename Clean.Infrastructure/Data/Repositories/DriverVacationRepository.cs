// ClassLibrary1/Repositories/DriverVacationRepository.cs

using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class DriverVacationRepository : IDriverVacationRepository
{
    private readonly DataContext _context;

    public DriverVacationRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<DriverVacation?> GetByIdAsync(int id)
    {
        return await _context.DriverVacations
            .Include(v => v.Driver)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<IEnumerable<DriverVacation>> GetByDriverIdAsync(int driverId)
    {
        return await _context.DriverVacations
            .Where(v => v.DriverId == driverId)
            .OrderByDescending(v => v.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<DriverVacation>> GetAllAsync()
    {
        return await _context.DriverVacations
            .Include(v => v.Driver)
            .OrderByDescending(v => v.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<DriverVacation>> GetActiveVacationsAsync(DateTime date)
    {
        var dateOnly = date.Date;
        return await _context.DriverVacations
            .Include(v => v.Driver)
            .Where(v => v.StartDate.Date <= dateOnly && v.EndDate.Date >= dateOnly)
            .ToListAsync();
    }

    public async Task<IEnumerable<DriverVacation>> GetVacationsInRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.DriverVacations
            .Include(v => v.Driver)
            .Where(v => v.StartDate.Date <= endDate.Date && v.EndDate.Date >= startDate.Date)
            .OrderBy(v => v.StartDate)
            .ToListAsync();
    }

    public async Task<DriverVacation> AddAsync(DriverVacation vacation)
    {
        _context.DriverVacations.Add(vacation);
        await _context.SaveChangesAsync();
        return vacation;
    }

    public async Task<DriverVacation?> UpdateAsync(DriverVacation vacation)
    {
        _context.DriverVacations.Update(vacation);
        await _context.SaveChangesAsync();
        return vacation;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var vacation = await _context.DriverVacations.FindAsync(id);
        if (vacation == null) return false;
        
        _context.DriverVacations.Remove(vacation);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsDriverOnVacationAsync(int driverId, DateTime date)
    {
        var dateOnly = date.Date;
        return await _context.DriverVacations
            .AnyAsync(v => v.DriverId == driverId && 
                          v.StartDate.Date <= dateOnly && 
                          v.EndDate.Date >= dateOnly);
    }

    public async Task<bool> HasOverlappingVacationAsync(int driverId, DateTime startDate, DateTime endDate, int? excludeId = null)
    {
        var query = _context.DriverVacations
            .Where(v => v.DriverId == driverId &&
                       v.StartDate.Date <= endDate.Date &&
                       v.EndDate.Date >= startDate.Date);

        if (excludeId.HasValue)
        {
            query = query.Where(v => v.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}