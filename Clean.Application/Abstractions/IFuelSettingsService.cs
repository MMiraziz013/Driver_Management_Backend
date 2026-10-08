using Clean.Application.Dtos.Fuel;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IFuelSettingsService
{
    Task<Response<FuelSettingsDto>> GetSettingsAsync();
    Task<Response<FuelSettingsDto>> UpdateSettingsAsync(UpdateFuelSettingsDto dto);
}
