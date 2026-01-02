using Clean.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportController : Controller
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Phase 1: Upload the CSV/Excel file for a specific 15-day period.
    /// </summary>
    [HttpPost("upload/{periodId}")]
    public async Task<IActionResult> UploadReportAsync(IFormFile file, int periodId)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var response = await _reportService.UploadReportAsync(file, periodId);
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Phase 2: Trigger the Auto-Assignment engine.
    /// </summary>
    [HttpPost("assign/{periodId}")]
    public async Task<IActionResult> RunAssignmentAsync(int periodId)
    {
        var response = await _reportService.RunAutoAssignmentAsync(periodId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPeriodsAsync()
    {
        var response = await _reportService.GetAllPeriods();
        return StatusCode(response.StatusCode, response);
    }

    /// <summary>
    /// Phase 3: Download the final Excel with driver assignments and conflict highlights.
    /// </summary>
    [HttpGet("export/{periodId}")]
    public async Task<IActionResult> ExportReportAsync(int periodId)
    {
        try
        {
            var fileBytes = await _reportService.ExportReportAsync(periodId);
            var fileName = $"Assignment_Report_Period_{periodId}.xlsx";
            
            if (fileBytes.Length == 0)
            {
                // Return 404 or 204 so the frontend knows there's no file
                return NotFound("No data found for this period. Did you run the assignment engine?");
            }
            
            return File(
                fileBytes, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName
            );
        }
        catch (Exception ex)
        {
            // Note: Standardize this with your error handling logic
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}