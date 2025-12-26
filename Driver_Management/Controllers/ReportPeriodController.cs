using Clean.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/report-periods")]
public class ReportPeriodController : Controller
{
    private readonly IReportPeriodService _periodService;

    public ReportPeriodController(IReportPeriodService periodService)
    {
        _periodService = periodService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(string description, DateTime startDate, DateTime endDate)
    {
        var response = await _periodService.CreatePeriodAsync(description, startDate, endDate);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var response = await _periodService.GetAllPeriodsAsync();
        return StatusCode(response.StatusCode, response);
    }
}