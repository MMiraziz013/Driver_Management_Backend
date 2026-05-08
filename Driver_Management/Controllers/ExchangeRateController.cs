using Clean.Application.Abstractions;
using Clean.Application.Dtos.Accounting;
using Clean.Application.Dtos.ExchangeRate;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/exchange-rates")]
public class ExchangeRateController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;

    public ExchangeRateController(IExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _exchangeRateService.GetAllAsync();
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("{year}")]
    public async Task<IActionResult> GetByYear(int year)
    {
        var result = await _exchangeRateService.GetByYearAsync(year);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate([FromBody] UpdateExchangeRateDto dto)
    {
        var result = await _exchangeRateService.CreateOrUpdateAsync(dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("{year}")]
    public async Task<IActionResult> Delete(int year)
    {
        var result = await _exchangeRateService.DeleteAsync(year);
        return StatusCode((int)result.StatusCode, result);
    }
}