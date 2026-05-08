using Clean.Application.Dtos.Accounting;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IFarmOutReportService
{
    Task<Response<FarmOutReportDto>> GenerateReportAsync(FarmOutReportRequestDto request);
    Task<Response<byte[]>> ExportToExcelAsync(FarmOutReportRequestDto request);
}