using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface ICachedLocationRepository
{
    Task<CachedLocation?> GetByAddressAsync(string address);
    Task<List<CachedLocation>> GetByAddressesAsync(IEnumerable<string> addresses);

    Task AddAsync(CachedLocation location);

}