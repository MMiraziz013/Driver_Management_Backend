using Clean.Application.Dtos.Responses;
using Microsoft.AspNetCore.Http;

namespace Clean.Application.Abstractions;

public interface IReportService
{
    Task<Response<string>> UploadReportAsync(IFormFile file, int periodId);
    Task<Response<string>> RunAutoAssignmentAsync(int periodId);
    Task<byte[]> ExportReportAsync(int periodId); // Binary data for the file
}