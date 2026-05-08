using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IExchangeRateRepository
{
    Task<ExchangeRate?> GetByYearAsync(int year);
    Task<List<ExchangeRate>> GetAllAsync();
    Task<ExchangeRate?> GetByIdAsync(int id);
    Task AddAsync(ExchangeRate rate);
    void Update(ExchangeRate rate);
    void Delete(ExchangeRate rate);
}