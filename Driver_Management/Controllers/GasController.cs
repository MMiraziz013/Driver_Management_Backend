using Clean.Application.Abstractions;
using Clean.Application.Dtos.Fuel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class GasController : ControllerBase
{
    private readonly IGasService _gasService;

    public GasController(IGasService gasService)
    {
        _gasService = gasService;
    }

    // ===== GAS PURCHASE MANAGEMENT =====

    /// <summary>
    /// Upload gas purchases from Excel report
    /// Expected columns: Date, Gas (liters), Type (АИ-92/АИ-95/ДТ), Amount (UZS)
    /// </summary>
    [HttpPost("purchases/upload/{periodId}")]
    public async Task<IActionResult> UploadGasPurchases(IFormFile file, int periodId)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var result = await _gasService.UploadGasPurchasesAsync(file, periodId);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Get all gas purchases for a period
    /// </summary>
    [HttpGet("purchases/{periodId}")]
    public async Task<IActionResult> GetGasPurchases(int periodId)
    {
        var result = await _gasService.GetGasPurchasesAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Get gas purchase summary by fuel type
    /// </summary>
    [HttpGet("purchases/{periodId}/summary")]
    public async Task<IActionResult> GetGasPurchaseSummary(int periodId)
    {
        var result = await _gasService.GetGasPurchaseSummaryAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Delete all gas purchases for a period
    /// </summary>
    [HttpDelete("purchases/{periodId}")]
    public async Task<IActionResult> DeleteGasPurchases(int periodId)
    {
        var result = await _gasService.DeleteGasPurchasesAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }

    // ===== VEHICLE FUEL CONFIGURATION =====

    /// <summary>
    /// Get all vehicles with their fuel configurations
    /// </summary>
    [HttpGet("vehicles/config")]
    public async Task<IActionResult> GetVehicleFuelConfigs()
    {
        var result = await _gasService.GetVehicleFuelConfigsAsync();
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Update a single vehicle's fuel configuration
    /// </summary>
    [HttpPut("vehicles/config")]
    public async Task<IActionResult> UpdateVehicleFuelConfig([FromBody] UpdateVehicleFuelConfigRequest request)
    {
        var result = await _gasService.UpdateVehicleFuelConfigAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Bulk update vehicle fuel configurations
    /// </summary>
    [HttpPut("vehicles/config/bulk")]
    public async Task<IActionResult> BulkUpdateVehicleFuelConfig([FromBody] List<UpdateVehicleFuelConfigRequest> requests)
    {
        var result = await _gasService.BulkUpdateVehicleFuelConfigAsync(requests);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Set initial fuel level for a vehicle
    /// </summary>
    [HttpPost("vehicles/initial-fuel")]
    public async Task<IActionResult> SetInitialFuelLevel([FromBody] SetInitialFuelLevelRequest request)
    {
        var result = await _gasService.SetInitialFuelLevelAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    // ===== FUEL ALLOCATION =====

    /// <summary>
    /// Run automatic fuel allocation for a period
    /// Distributes gas purchases to vehicles based on distance driven
    /// </summary>
    [HttpPost("allocate/{periodId}")]
    public async Task<IActionResult> RunAutoFuelAllocation(int periodId)
    {
        var result = await _gasService.RunAutoFuelAllocationAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Preview fuel allocation without saving (dry run)
    /// </summary>
    [HttpGet("allocate/{periodId}/preview")]
    public async Task<IActionResult> PreviewFuelAllocation(int periodId)
    {
        var result = await _gasService.PreviewFuelAllocationAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Manually allocate fuel from a purchase to a vehicle
    /// </summary>
    [HttpPost("allocate/manual")]
    public async Task<IActionResult> ManualFuelAllocation([FromBody] ManualFuelAllocationRequest request)
    {
        var result = await _gasService.ManualFuelAllocationAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    // ===== STATUS & VALIDATION =====

    /// <summary>
    /// Get fuel status for all vehicles in a period
    /// </summary>
    [HttpGet("status/{periodId}")]
    public async Task<IActionResult> GetVehicleFuelStatus(int periodId)
    {
        var result = await _gasService.GetVehicleFuelStatusAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Get detailed daily fuel balance for a specific vehicle
    /// </summary>
    [HttpGet("balance/{vehicleId}/{periodId}")]
    public async Task<IActionResult> GetVehicleFuelBalance(int vehicleId, int periodId)
    {
        var result = await _gasService.GetVehicleFuelBalanceAsync(vehicleId, periodId);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Validate fuel allocations for a period (check for issues)
    /// </summary>
    [HttpGet("validate/{periodId}")]
    public async Task<IActionResult> ValidateFuelAllocations(int periodId)
    {
        var result = await _gasService.ValidateFuelAllocationsAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Get fuel cost breakdown by vehicle
    /// </summary>
    [HttpGet("costs/{periodId}")]
    public async Task<IActionResult> GetFuelCostBreakdown(int periodId)
    {
        var result = await _gasService.GetFuelCostBreakdownAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }
    
    /// <summary>
    /// Preview what finalization would do (dry run).
    /// Shows how vehicle initial levels would be updated without saving.
    /// </summary>
    [HttpGet("finalize/{periodId}/preview")]
    public async Task<IActionResult> PreviewFinalization(int periodId)
    {
        var result = await _gasService.PreviewFinalizationAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Finalize fuel allocation for a period.
    /// This confirms the allocation and updates vehicle initial fuel levels for the next period.
    /// WARNING: This action affects future periods!
    /// </summary>
    [HttpPost("finalize/{periodId}")]
    public async Task<IActionResult> FinalizeFuelAllocation(int periodId)
    {
        var result = await _gasService.FinalizeFuelAllocationAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }

    /// <summary>
    /// Revert a finalized period (unlock it).
    /// Note: Vehicle initial levels may need manual correction after revert.
    /// </summary>
    [HttpPost("finalize/{periodId}/revert")]
    public async Task<IActionResult> RevertFinalization(int periodId)
    {
        var result = await _gasService.RevertFinalizationAsync(periodId);
        return StatusCode((int)result.StatusCode, result);
    }


    /// <summary>
    /// Get purchased fuel diagnostic by type
    /// </summary>
    [HttpGet("fuel-diagnostic/{periodId}")]
    public async Task<IActionResult> GetFuelDiagnosticAsync(int periodId)
    {
        var response = await _gasService.GetFuelDiagnosticsAsync(periodId);
        return StatusCode(response.StatusCode, response);
    }

    // ===== EXPORT =====

    /// <summary>
    /// Export fuel report to Excel
    /// </summary>
    [HttpGet("export/{periodId}")]
    public async Task<IActionResult> ExportFuelReport(int periodId)
    {
        var fileBytes = await _gasService.ExportFuelReportAsync(periodId);
        
        if (fileBytes.Length == 0)
            return NotFound("No data to export");

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"FuelReport_Period_{periodId}_{DateTime.Now:yyyyMMdd}.xlsx"
        );
    }
    
    /// <summary>
    /// Export fuel allocation details to CSV (zipped)
    /// </summary>
    /// <param name="periodId">Report period ID</param>
    /// <returns>ZIP file containing CSV files with detailed breakdown</returns>
    [HttpGet("export-detailed/{periodId}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportFuelAllocation(int periodId)
    {
        var response = await _gasService.ExportFuelAllocationToCsvAsync(periodId);
        
        return File(
            response.Data!.FileContent, 
            response.Data.ContentType, 
            response.Data.FileName
        );
    }

}
