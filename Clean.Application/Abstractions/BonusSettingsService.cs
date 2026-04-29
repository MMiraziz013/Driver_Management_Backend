using Clean.Application.Dtos;
using Clean.Application.Dtos.Bonus;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IBonusSettingsService
{
    Task<Response<BonusSettingsDto>> GetActiveSettingsAsync();
    Task<Response<BonusSettingsDto>> UpdateSettingsAsync(UpdateBonusSettingsDto dto);
    Task<Response<List<ServiceTypeBonusConfigDto>>> GetServiceTypeConfigsAsync();
    Task<Response<ServiceTypeBonusConfigDto>> UpdateServiceTypeConfigAsync(UpdateServiceTypeBonusConfigDto dto);
    Task<Response<string>> InitializeDefaultConfigsAsync();
}