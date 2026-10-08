using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IFuelSettingsRepository
{
    Task<FuelSettings?> GetAsync();
    Task<double> GetDurationFallbackSpeedKmhAsync();
    Task AddAsync(FuelSettings settings);
    void Update(FuelSettings settings);
}
