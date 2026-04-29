using Clean.Application.Abstractions;
using Clean.Application.Dtos.Bonus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/bonuses")]
[Authorize]
public class BonusCalculationController : ControllerBase
{
    private readonly IBonusCalculationService _bonusCalculationService;

    public BonusCalculationController(IBonusCalculationService bonusCalculationService)
    {
        _bonusCalculationService = bonusCalculationService;
    }

    /// <summary>
    /// Calculate bonuses for selected periods
    /// </summary>
    [HttpPost("calculate")]
    public async Task<IActionResult> CalculateBonuses([FromBody] BonusCalculationRequestDto request)
    {
        var response = await _bonusCalculationService.CalculateBonusesAsync(request);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Export bonuses to Excel
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> ExportBonuses([FromBody] BonusCalculationRequestDto request)
    {
        var response = await _bonusCalculationService.ExportBonusesToExcelAsync(request);
        
        if (response.StatusCode != 200 || response.Data == null)
        {
            return StatusCode(response.StatusCode, response);
        }

        var fileName = $"Bonuses_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";
        return File(response.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}