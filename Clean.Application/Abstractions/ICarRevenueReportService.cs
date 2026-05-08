using Clean.Application.Dtos.Accounting;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface ICarRevenueReportService
{
    Task<Response<CarRevenueReportDto>> GenerateReportAsync(CarRevenueReportRequestDto request);
    Task<Response<byte[]>> ExportToExcelAsync(CarRevenueReportRequestDto request);
}