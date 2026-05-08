using Clean.Application.Dtos.Accounting;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IAnalysisReportService
{
    Task<Response<AnalysisReportDto>> GenerateReportAsync(AnalysisReportRequestDto request);
    Task<Response<byte[]>> ExportToExcelAsync(AnalysisReportRequestDto request);
}