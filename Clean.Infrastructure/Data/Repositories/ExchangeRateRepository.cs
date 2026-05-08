using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly DataContext _context;

    public ExchangeRateRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<ExchangeRate?> GetByYearAsync(int year)
    {
        return await _context.ExchangeRates
            .FirstOrDefaultAsync(e => e.Year == year);
    }

    public async Task<List<ExchangeRate>> GetAllAsync()
    {
        return await _context.ExchangeRates
            .OrderByDescending(e => e.Year)
            .ToListAsync();
    }

    public async Task<ExchangeRate?> GetByIdAsync(int id)
    {
        return await _context.ExchangeRates.FindAsync(id);
    }

    public async Task AddAsync(ExchangeRate rate)
    {
        await _context.ExchangeRates.AddAsync(rate);
    }

    public void Update(ExchangeRate rate)
    {
        _context.ExchangeRates.Update(rate);
    }

    public void Delete(ExchangeRate rate)
    {
        _context.ExchangeRates.Remove(rate);
    }
}