using Clean.Application.Dtos.Fuel;
using Clean.Application.Dtos.Responses;
using Microsoft.AspNetCore.Http;

namespace Clean.Application.Abstractions;

/// <summary>
/// Service interface for gas/fuel management operations
/// </summary>
public interface IGasService
{
    // ===== GAS PURCHASE MANAGEMENT =====

    /// <summary>
    /// Upload gas purchases from an Excel report
    /// Expected columns: Date, Gas (liters), Type (АИ-92/АИ-95/ДТ), Amount (UZS)
    /// </summary>
    Task<Response<GasPurchaseSummaryDto>> UploadGasPurchasesAsync(IFormFile file, int periodId);

    /// <summary>
    /// Get all gas purchases for a period
    /// </summary>
    Task<Response<List<GasPurchaseDto>>> GetGasPurchasesAsync(int periodId);

    /// <summary>
    /// Get gas purchase summary by fuel type for a period
    /// </summary>
    Task<Response<GasPurchaseSummaryDto>> GetGasPurchaseSummaryAsync(int periodId);

    /// <summary>
    /// Delete all gas purchases for a period (before re-upload)
    /// </summary>
    Task<Response<string>> DeleteGasPurchasesAsync(int periodId);

    // ===== VEHICLE FUEL CONFIGURATION =====

    /// <summary>
    /// Update vehicle fuel configuration (tank capacity, consumption rate, fuel type)
    /// </summary>
    Task<Response<string>> UpdateVehicleFuelConfigAsync(UpdateVehicleFuelConfigRequest request);

    /// <summary>
    /// Bulk update vehicle fuel configurations
    /// </summary>
    Task<Response<string>> BulkUpdateVehicleFuelConfigAsync(List<UpdateVehicleFuelConfigRequest> requests);

    /// <summary>
    /// Get all vehicles with their fuel configurations
    /// </summary>
    Task<Response<List<VehicleFuelStatusDto>>> GetVehicleFuelConfigsAsync();

    /// <summary>
    /// Set initial fuel level for a vehicle at the start of a period
    /// </summary>
    Task<Response<string>> SetInitialFuelLevelAsync(SetInitialFuelLevelRequest request);

    // ===== FUEL ALLOCATION & CALCULATION =====

    /// <summary>
    /// Run automatic fuel allocation for a period
    /// This distributes gas purchases to vehicles based on distance driven
    /// </summary>
    Task<Response<FuelCalculationResultDto>> RunAutoFuelAllocationAsync(int periodId);

    /// <summary>
    /// Preview fuel allocation without saving (dry run)
    /// </summary>
    Task<Response<FuelCalculationResultDto>> PreviewFuelAllocationAsync(int periodId);

    /// <summary>
    /// Manually allocate fuel from a purchase to a vehicle
    /// </summary>
    Task<Response<string>> ManualFuelAllocationAsync(ManualFuelAllocationRequest request);

    /// <summary>
    /// Get fuel status for all vehicles in a period
    /// </summary>
    Task<Response<List<VehicleFuelStatusDto>>> GetVehicleFuelStatusAsync(int periodId);

    /// <summary>
    /// Get detailed fuel balance timeline for a specific vehicle
    /// </summary>
    Task<Response<FuelBalancePreviewDto>> GetVehicleFuelBalanceAsync(int vehicleId, int periodId);

    /// <summary>
    /// Validate fuel allocations for a period (check for issues)
    /// </summary>
    Task<Response<FuelCalculationResultDto>> ValidateFuelAllocationsAsync(int periodId);
    
    Task<Response<FuelFinalizationResultDto>> PreviewFinalizationAsync(int periodId);
    
    Task<Response<FuelFinalizationResultDto>> FinalizeFuelAllocationAsync(int periodId);
    
    Task<Response<string>> RevertFinalizationAsync(int periodId);

    /// <summary>
    /// Get detailed information about purchased fuel's type, amount and demand for vehicles
    /// </summary>
    Task<Response<FuelDiagnosticDto>> GetFuelDiagnosticsAsync(int periodId);

    // ===== REPORTING =====

    /// <summary>
    /// Export fuel report to Excel
    /// </summary>
    Task<byte[]> ExportFuelReportAsync(int periodId);

    Task<Response<FuelAllocationExportDto>> ExportFuelAllocationToCsvAsync(int periodId);

    /// <summary>
    /// Get fuel cost breakdown by vehicle for a period
    /// </summary>
    Task<Response<List<VehicleFuelStatusDto>>> GetFuelCostBreakdownAsync(int periodId);
}
