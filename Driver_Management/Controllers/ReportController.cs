using Clean.Application.Abstractions;
using Clean.Application.Dtos.Report;
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
    
    /// <summary>
    /// Preview period finalization (both fuel and driver assignments).
    /// </summary>
    [HttpGet("periods/{periodId}/finalize/preview")]
    public async Task<IActionResult> PreviewPeriodFinalization(int periodId)
    {
        var result = await _reportService.PreviewPeriodFinalizationAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Finalize an entire period - updates vehicle fuel levels and driver states
    /// for use in the next period.
    /// WARNING: This action affects future periods!
    /// </summary>
    [HttpPost("periods/{periodId}/finalize")]
    public async Task<IActionResult> FinalizePeriod(int periodId)
    {
        // Could get userId from JWT claims if using authentication
        string? userId = User?.Identity?.Name;
    
        var result = await _reportService.FinalizePeriodAsync(periodId, userId);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Revert period finalization (unlock for corrections).
    /// </summary>
    [HttpPost("periods/{periodId}/finalize/revert")]
    public async Task<IActionResult> RevertPeriodFinalization(int periodId)
    {
        var result = await _reportService.RevertPeriodFinalizationAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }
    
    /// <summary>
    /// Export waybill report (Путевой лист) with grouped journeys
    /// </summary>
    [HttpGet("export-waybill/{periodId}")]
    public async Task<IActionResult> ExportWaybillReport(int periodId)
    {
        var bytes = await _reportService.GetWaybillReportAsync(periodId);
        
        if (bytes.Data != null && bytes.Data.Length == 0)
        {
            return NotFound("Period not found or has no data");
        }
        
        var period = await _reportService.GetAllPeriods();
        var periodInfo = period.Data?.FirstOrDefault(p => p.Id == periodId);
        var fileName = periodInfo != null 
            ? $"Путевой_лист_{periodInfo.StartDate:yyyy-MM-dd}_{periodInfo.EndDate:yyyy-MM-dd}.xlsx"
            : $"Путевой_лист_{periodId}.xlsx";
        
        return File(bytes.Data!, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            fileName);
    }

    /// <summary>
    /// Get journeys (grouped trips) for a period
    /// </summary>
    [HttpGet("journeys/{periodId}")]
    public async Task<ActionResult<List<JourneyDto>>> GetJourneys(int periodId)
    {
        var journeys = await _reportService.GetAllJourneysAsync(periodId);
        return Ok(journeys);
    }

    /// <summary>
    /// Update vehicle mileage
    /// </summary>
    [HttpPut("vehicle/{vehicleId}/mileage")]
    public async Task<IActionResult> UpdateVehicleMileage(int vehicleId, [FromBody] UpdateMileageRequest request)
    {
        var result = await _reportService.UpdateVehicleMileageAsync(vehicleId, request.NewMileage);
        
        if (result.StatusCode == (int)System.Net.HttpStatusCode.OK)
        {
            return Ok(new { message = result.Message });
        }
        
        return StatusCode((int)result.StatusCode, new { message = result.Message });
    }

    /// <summary>
    /// Bulk update vehicle mileages
    /// </summary>
    [HttpPut("vehicles/mileage/bulk")]
    public async Task<IActionResult> BulkUpdateVehicleMileages([FromBody] BulkMileageUpdateRequest request)
    {
        var updates = request.Updates
            .Select(u => (u.VehicleId, u.NewMileage))
            .ToList();
        
        var result = await _reportService.BulkUpdateVehicleMileagesAsync(updates);
        
        if (result.StatusCode == (int)System.Net.HttpStatusCode.OK)
        {
            return Ok(new { message = result.Message });
        }
        
        return StatusCode((int)result.StatusCode, new { message = result.Message });
    }
}