using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class AccountingTransactionRepository : IAccountingTransactionRepository
{
    private readonly DataContext _context;

    public AccountingTransactionRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<List<AccountingTransaction>> GetByReportIdAsync(int reportId)
    {
        return await _context.AccountingTransactions
            .Where(t => t.AccountingReportId == reportId)
            .ToListAsync();
    }

    public async Task<List<AccountingTransaction>> GetByYearAsync(int year)
    {
        return await _context.AccountingTransactions
            .Where(t => t.Year == year)
            .OrderBy(t => t.Month)
            .ToListAsync();
    }

    public async Task<List<AccountingTransaction>> GetByYearAndMonthAsync(int year, int month)
    {
        return await _context.AccountingTransactions
            .Where(t => t.Year == year && t.Month == month)
            .ToListAsync();
    }

    public async Task<List<AccountingTransaction>> GetByYearAndTypeAsync(int year, TransactionType type)
    {
        return await _context.AccountingTransactions
            .Where(t => t.Year == year && t.Type == type)
            .OrderBy(t => t.Month)
            .ToListAsync();
    }

    public async Task<List<AccountingTransaction>> GetByYearsAsync(List<int> years)
    {
        return await _context.AccountingTransactions
            .Where(t => years.Contains(t.Year))
            .OrderBy(t => t.Year)
            .ThenBy(t => t.Month)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<AccountingTransaction> transactions)
    {
        await _context.AccountingTransactions.AddRangeAsync(transactions);
    }

    public void DeleteRange(IEnumerable<AccountingTransaction> transactions)
    {
        _context.AccountingTransactions.RemoveRange(transactions);
    }
}