using Clean.Application.Dtos.Accounting;
using Clean.Application.Dtos.ExchangeRate;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IExchangeRateService
{
    Task<Response<List<ExchangeRateDto>>> GetAllAsync();
    Task<Response<ExchangeRateDto>> GetByYearAsync(int year);
    Task<Response<ExchangeRateDto>> CreateOrUpdateAsync(UpdateExchangeRateDto dto);
    Task<Response<string>> DeleteAsync(int year);
}