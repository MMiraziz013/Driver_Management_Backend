using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IReportPeriodRepository
{
    Task<ReportPeriod?> GetByIdAsync(int id);
    Task<List<ReportPeriod>> GetAllAsync();
    Task AddAsync(ReportPeriod period);
    void Update(ReportPeriod period);
    void Delete(ReportPeriod period);
    
    // Your existing specialized methods
    Task<ReportPeriod?> GetWithTripsAsync(int id);
    Task<ReportPeriod?> GetWithAssignmentsAsync(int id);
}