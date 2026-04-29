using Clean.Application.Abstractions;
using Clean.Application.Dtos.Bonus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/bonus-settings")]
[Authorize]
public class BonusSettingsController : ControllerBase
{
    private readonly IBonusSettingsService _bonusSettingsService;

    public BonusSettingsController(IBonusSettingsService bonusSettingsService)
    {
        _bonusSettingsService = bonusSettingsService;
    }

    /// <summary>
    /// Get active bonus settings (rates, premium vehicle types)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var response = await _bonusSettingsService.GetActiveSettingsAsync();
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Update bonus settings (rates, premium vehicle types)
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateBonusSettingsDto dto)
    {
        var response = await _bonusSettingsService.UpdateSettingsAsync(dto);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Get all service type bonus configurations
    /// </summary>
    [HttpGet("service-types")]
    public async Task<IActionResult> GetServiceTypeConfigs()
    {
        var response = await _bonusSettingsService.GetServiceTypeConfigsAsync();
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Update calculation method for a service type
    /// </summary>
    [HttpPut("service-types")]
    public async Task<IActionResult> UpdateServiceTypeConfig([FromBody] UpdateServiceTypeBonusConfigDto dto)
    {
        var response = await _bonusSettingsService.UpdateServiceTypeConfigAsync(dto);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Initialize default configs for all service types
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeDefaults()
    {
        var response = await _bonusSettingsService.InitializeDefaultConfigsAsync();
        return StatusCode(response.StatusCode, response);
    }
}