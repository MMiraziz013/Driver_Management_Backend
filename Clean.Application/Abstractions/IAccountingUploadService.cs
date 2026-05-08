using Clean.Application.Dtos.Accounting;
using Clean.Application.Dtos.Responses;
using Microsoft.AspNetCore.Http;

namespace Clean.Application.Abstractions;

public interface IAccountingUploadService
{
    Task<Response<AccountingUploadResultDto>> UploadReportAsync(IFormFile file, int year, int month, string? uploadedBy = null);
    Task<Response<List<AccountingReportDto>>> GetAllReportsAsync();
    Task<Response<List<AccountingReportDto>>> GetReportsByYearAsync(int year);
    Task<Response<string>> DeleteReportAsync(int year, int month);
}