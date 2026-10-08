using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class FuelSettingsRepository : IFuelSettingsRepository
{
    private readonly DataContext _context;

    public FuelSettingsRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<FuelSettings?> GetAsync()
    {
        return await _context.FuelSettings
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<double> GetDurationFallbackSpeedKmhAsync()
    {
        var settings = await _context.FuelSettings
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync();

        return settings?.DurationFallbackSpeedKmh ?? FuelSettings.DefaultDurationFallbackSpeedKmh;
    }

    public async Task AddAsync(FuelSettings settings)
    {
        await _context.FuelSettings.AddAsync(settings);
    }

    public void Update(FuelSettings settings)
    {
        _context.FuelSettings.Update(settings);
    }
}
