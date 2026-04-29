using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class BonusSettingsRepository : IBonusSettingsRepository
{
    private readonly DataContext _context;

    public BonusSettingsRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<BonusSettings?> GetActiveAsync()
    {
        return await _context.BonusSettings
            .FirstOrDefaultAsync(s => s.IsActive);
    }

    public async Task<BonusSettings?> GetByIdAsync(int id)
    {
        return await _context.BonusSettings
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task AddAsync(BonusSettings settings)
    {
        await _context.BonusSettings.AddAsync(settings);
    }

    public void Update(BonusSettings settings)
    {
        _context.BonusSettings.Update(settings);
    }
}