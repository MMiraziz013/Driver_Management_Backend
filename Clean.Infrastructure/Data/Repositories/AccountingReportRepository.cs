using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class AccountingReportRepository : IAccountingReportRepository
{
    private readonly DataContext _context;

    public AccountingReportRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<AccountingReport?> GetByIdAsync(int id)
    {
        return await _context.AccountingReports
            .Include(r => r.Transactions)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<AccountingReport?> GetByYearAndMonthAsync(int year, int month)
    {
        return await _context.AccountingReports
            .Include(r => r.Transactions)
            .FirstOrDefaultAsync(r => r.Year == year && r.Month == month);
    }

    public async Task<List<AccountingReport>> GetByYearAsync(int year)
    {
        return await _context.AccountingReports
            .Where(r => r.Year == year)
            .OrderBy(r => r.Month)
            .ToListAsync();
    }

    public async Task<List<AccountingReport>> GetAllAsync()
    {
        return await _context.AccountingReports
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Month)
            .ToListAsync();
    }

    public async Task AddAsync(AccountingReport report)
    {
        await _context.AccountingReports.AddAsync(report);
    }

    public void Update(AccountingReport report)
    {
        _context.AccountingReports.Update(report);
    }

    public void Delete(AccountingReport report)
    {
        _context.AccountingReports.Remove(report);
    }
}