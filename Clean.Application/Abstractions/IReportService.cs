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
}   