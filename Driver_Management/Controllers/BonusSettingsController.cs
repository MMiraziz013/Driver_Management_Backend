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

    /// <summary>
    /// Get all calculation exceptions (e.g. Sierra Nevada Round Trips via Chirchik => hourly)
    /// </summary>
    [HttpGet("exceptions")]
    public async Task<IActionResult> GetExceptions()
    {
        var response = await _bonusSettingsService.GetExceptionsAsync();
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Get a calculation exception by id
    /// </summary>
    [HttpGet("exceptions/{id:int}")]
    public async Task<IActionResult> GetException(int id)
    {
        var response = await _bonusSettingsService.GetExceptionByIdAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Create a calculation exception
    /// </summary>
    [HttpPost("exceptions")]
    public async Task<IActionResult> CreateException([FromBody] SaveBonusCalculationExceptionDto dto)
    {
        var response = await _bonusSettingsService.CreateExceptionAsync(dto);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Update a calculation exception (company, service type, method, vehicle types, location keywords)
    /// </summary>
    [HttpPut("exceptions/{id:int}")]
    public async Task<IActionResult> UpdateException(int id, [FromBody] SaveBonusCalculationExceptionDto dto)
    {
        var response = await _bonusSettingsService.UpdateExceptionAsync(id, dto);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Delete a calculation exception
    /// </summary>
    [HttpDelete("exceptions/{id:int}")]
    public async Task<IActionResult> DeleteException(int id)
    {
        var response = await _bonusSettingsService.DeleteExceptionAsync(id);
        return StatusCode(response.StatusCode, response);
    }
}