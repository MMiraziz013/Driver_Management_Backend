using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface ICachedLocationRepository
{
    Task<CachedLocation?> GetByAddressAsync(string address);
    Task AddAsync(CachedLocation location);

}