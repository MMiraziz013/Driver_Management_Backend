using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IAccountingReportRepository
{
    Task<AccountingReport?> GetByIdAsync(int id);
    Task<AccountingReport?> GetByYearAndMonthAsync(int year, int month);
    Task<List<AccountingReport>> GetByYearAsync(int year);
    Task<List<AccountingReport>> GetAllAsync();
    Task AddAsync(AccountingReport report);
    void Update(AccountingReport report);
    void Delete(AccountingReport report);
}