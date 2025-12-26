using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IReportPeriodService
{
    Task<Response<ReportPeriod>> CreatePeriodAsync(string name, DateTime start, DateTime end);
    Task<Response<List<ReportPeriod>>> GetAllPeriodsAsync();
    Task<Response<ReportPeriod>> GetPeriodByIdAsync(int id);
}