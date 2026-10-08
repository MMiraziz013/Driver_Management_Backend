using Clean.Application.Abstractions;
using Clean.Application.Dtos.Fuel;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/fuel-settings")]
public class FuelSettingsController : ControllerBase
{
    private readonly IFuelSettingsService _fuelSettingsService;

    public FuelSettingsController(IFuelSettingsService fuelSettingsService)
    {
        _fuelSettingsService = fuelSettingsService;
    }

    /// <summary>
    /// Get fuel settings (duration-based distance fallback speed)
    /// </summary>
    [HttpGet]
    [PermissionAuthorize(PermissionConstants.Gas.View)]
    public async Task<IActionResult> GetSettings()
    {
        var response = await _fuelSettingsService.GetSettingsAsync();
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Update fuel settings (duration-based distance fallback speed)
    /// </summary>
    [HttpPut]
    [PermissionAuthorize(PermissionConstants.Gas.Manage)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateFuelSettingsDto dto)
    {
        var response = await _fuelSettingsService.UpdateSettingsAsync(dto);
        return StatusCode(response.StatusCode, response);
    }
}
