using Clean.Application.Dtos.Accounting;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface ICompanyRevenueReportService
{
    Task<Response<CompanyRevenueReportDto>> GenerateReportAsync(CompanyRevenueReportRequestDto request);
    Task<Response<byte[]>> ExportToExcelAsync(CompanyRevenueReportRequestDto request);
}