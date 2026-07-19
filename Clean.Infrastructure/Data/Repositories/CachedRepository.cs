using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class CachedLocationRepository : ICachedLocationRepository
{
    private readonly DataContext _context;

    public CachedLocationRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<CachedLocation?> GetByAddressAsync(string address)
    {
        return await _context.CachedLocations
            .FirstOrDefaultAsync(c => c.AddressName == address);
    }
    
    public async Task<List<CachedLocation>> GetByAddressesAsync(IEnumerable<string> addresses)
    {
        var keys = addresses.ToList();
        return await _context.CachedLocations
            .Where(c => keys.Contains(c.AddressName))
            .ToListAsync();
    }

    public async Task AddAsync(CachedLocation location)
    {
        await _context.CachedLocations.AddAsync(location);
    }
}
