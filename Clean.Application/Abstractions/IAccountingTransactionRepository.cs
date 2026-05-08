using Clean.Domain.Entities;
using Clean.Domain.Enums;

namespace Clean.Application.Abstractions;

public interface IAccountingTransactionRepository
{
    Task<List<AccountingTransaction>> GetByReportIdAsync(int reportId);
    Task<List<AccountingTransaction>> GetByYearAsync(int year);
    Task<List<AccountingTransaction>> GetByYearAndMonthAsync(int year, int month);
    Task<List<AccountingTransaction>> GetByYearAndTypeAsync(int year, TransactionType type);
    Task<List<AccountingTransaction>> GetByYearsAsync(List<int> years);
    Task AddRangeAsync(IEnumerable<AccountingTransaction> transactions);
    void DeleteRange(IEnumerable<AccountingTransaction> transactions);
}