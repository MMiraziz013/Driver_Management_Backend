using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class BonusCalculationExceptionRepository : IBonusCalculationExceptionRepository
{
    private readonly DataContext _context;

    public BonusCalculationExceptionRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<List<BonusCalculationException>> GetAllWithServiceTypeAsync()
    {
        return await _context.BonusCalculationExceptions
            .Include(e => e.ServiceType)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<List<BonusCalculationException>> GetActiveAsync()
    {
        return await _context.BonusCalculationExceptions
            .Include(e => e.ServiceType)
            .Where(e => e.IsActive)
            .OrderBy(e => e.Id)
            .ToListAsync();
    }

    public async Task<BonusCalculationException?> GetByIdAsync(int id)
    {
        return await _context.BonusCalculationExceptions
            .Include(e => e.ServiceType)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task AddAsync(BonusCalculationException exception)
    {
        await _context.BonusCalculationExceptions.AddAsync(exception);
    }

    public void Update(BonusCalculationException exception)
    {
        _context.BonusCalculationExceptions.Update(exception);
    }

    public void Remove(BonusCalculationException exception)
    {
        _context.BonusCalculationExceptions.Remove(exception);
    }
}
