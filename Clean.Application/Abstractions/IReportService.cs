using Clean.Application.Dtos.Report;
using Clean.Application.Dtos.ReportPeriod;
using Clean.Application.Dtos.Responses;
using Microsoft.AspNetCore.Http;

namespace Clean.Application.Abstractions;

public interface IReportService
{
    Task<Response<List<GetReportPeriodDto>>> GetAllPeriods();
    Task<Response<string>> UploadReportAsync(IFormFile file, int periodId);
    Task<Response<string>> RunAutoAssignmentAsync(int periodId);
    Task<byte[]> ExportReportAsync(int periodId); // Binary data for the file
    
    Task<Response<PeriodFinalizationResultDto>> PreviewPeriodFinalizationAsync(int periodId);

    Task<Response<PeriodFinalizationResultDto>> FinalizePeriodAsync(int periodId, string? userId = null);

    Task<Response<string>> RevertPeriodFinalizationAsync(int periodId);

    Task<Response<byte[]>> GetWaybillReportAsync(int periodId);

    Task<Response<List<JourneyDto>>> GetAllJourneysAsync(int periodId);
    
    /// <summary>
    /// Update a vehicle's current mileage
    /// </summary>
    Task<Response<string>> UpdateVehicleMileageAsync(int vehicleId, double newMileage);

    /// <summary>
    /// Bulk update vehicle mileages
    /// </summary>
    Task<Response<string>> BulkUpdateVehicleMileagesAsync(List<(int VehicleId, double NewMileage)> updates);

}   