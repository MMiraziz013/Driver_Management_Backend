using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Fuel;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;

namespace Clean.Application.Services.Fuel;

public class FuelSettingsService : IFuelSettingsService
{
    private const double MAX_DURATION_FALLBACK_SPEED_KMH = 150.0;

    private readonly IUnitOfWork _uow;

    public FuelSettingsService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<FuelSettingsDto>> GetSettingsAsync()
    {
        try
        {
            var settings = await GetOrCreateAsync();

            return new Response<FuelSettingsDto>(HttpStatusCode.OK, MapToDto(settings));
        }
        catch (Exception ex)
        {
            return new Response<FuelSettingsDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<FuelSettingsDto>> UpdateSettingsAsync(UpdateFuelSettingsDto dto)
    {
        try
        {
            if (dto.DurationFallbackSpeedKmh.HasValue &&
                (double.IsNaN(dto.DurationFallbackSpeedKmh.Value) ||
                 dto.DurationFallbackSpeedKmh.Value <= 0 ||
                 dto.DurationFallbackSpeedKmh.Value > MAX_DURATION_FALLBACK_SPEED_KMH))
            {
                return new Response<FuelSettingsDto>(HttpStatusCode.BadRequest,
                    new List<string>
                    {
                        $"DurationFallbackSpeedKmh must be greater than 0 and at most {MAX_DURATION_FALLBACK_SPEED_KMH}"
                    });
            }

            var settings = await GetOrCreateAsync();

            // Update only provided fields
            if (dto.DurationFallbackSpeedKmh.HasValue)
                settings.DurationFallbackSpeedKmh = dto.DurationFallbackSpeedKmh.Value;

            settings.UpdatedAt = DateTime.UtcNow;

            _uow.FuelSettings.Update(settings);
            await _uow.CompleteAsync();

            return new Response<FuelSettingsDto>(HttpStatusCode.OK, MapToDto(settings));
        }
        catch (Exception ex)
        {
            return new Response<FuelSettingsDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    private async Task<FuelSettings> GetOrCreateAsync()
    {
        var settings = await _uow.FuelSettings.GetAsync();

        if (settings == null)
        {
            // Create default settings if none exist
            settings = new FuelSettings();
            await _uow.FuelSettings.AddAsync(settings);
            await _uow.CompleteAsync();
        }

        return settings;
    }

    private static FuelSettingsDto MapToDto(FuelSettings settings) => new()
    {
        Id = settings.Id,
        DurationFallbackSpeedKmh = settings.DurationFallbackSpeedKmh,
        UpdatedAt = settings.UpdatedAt
    };
}
