using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.ExchangeRate;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Services.ExchangeRate;

public class ExchangeRateService : IExchangeRateService
{
    private readonly IUnitOfWork _uow;

    public ExchangeRateService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<List<ExchangeRateDto>>> GetAllAsync()
    {
        try
        {
            var rates = await _uow.ExchangeRates.GetAllAsync();
            var dtos = rates.Select(MapToDto).ToList();
            return new Response<List<ExchangeRateDto>>(HttpStatusCode.OK, dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<ExchangeRateDto>>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<ExchangeRateDto>> GetByYearAsync(int year)
    {
        try
        {
            var rate = await _uow.ExchangeRates.GetByYearAsync(year);
            if (rate == null)
            {
                return new Response<ExchangeRateDto>(HttpStatusCode.NotFound,
                    new List<string> { $"Exchange rate for year {year} not found" });
            }

            return new Response<ExchangeRateDto>(HttpStatusCode.OK, MapToDto(rate));
        }
        catch (Exception ex)
        {
            return new Response<ExchangeRateDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<ExchangeRateDto>> CreateOrUpdateAsync(UpdateExchangeRateDto dto)
    {
        try
        {
            var existing = await _uow.ExchangeRates.GetByYearAsync(dto.Year);

            if (existing != null)
            {
                existing.Rate = dto.Rate;
                existing.UpdatedAt = DateTime.UtcNow;
                _uow.ExchangeRates.Update(existing);
            }
            else
            {
                existing = new Domain.Entities.ExchangeRate
                {
                    Year = dto.Year,
                    Rate = dto.Rate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _uow.ExchangeRates.AddAsync(existing);
            }

            await _uow.CompleteAsync();

            return new Response<ExchangeRateDto>(HttpStatusCode.OK, 
                $"Exchange rate for {dto.Year} saved", MapToDto(existing));
        }
        catch (Exception ex)
        {
            return new Response<ExchangeRateDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<string>> DeleteAsync(int year)
    {
        try
        {
            var rate = await _uow.ExchangeRates.GetByYearAsync(year);
            if (rate == null)
            {
                return new Response<string>(HttpStatusCode.NotFound,
                    new List<string> { $"Exchange rate for year {year} not found" });
            }

            _uow.ExchangeRates.Delete(rate);
            await _uow.CompleteAsync();

            return new Response<string>(HttpStatusCode.OK, $"Exchange rate for {year} deleted");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    private static ExchangeRateDto MapToDto(Domain.Entities.ExchangeRate rate) => new()
    {
        Id = rate.Id,
        Year = rate.Year,
        Rate = rate.Rate,
        UpdatedAt = rate.UpdatedAt,
        UpdatedBy = rate.UpdatedBy
    };
}