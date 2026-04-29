using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Bonus;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;
using Clean.Domain.Enums;

namespace Clean.Application.Services.Bonus;

public class BonusSettingsService : IBonusSettingsService
{
    private readonly IUnitOfWork _uow;

    public BonusSettingsService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<BonusSettingsDto>> GetActiveSettingsAsync()
    {
        try
        {
            var settings = await _uow.BonusSettings.GetActiveAsync();

            if (settings == null)
            {
                // Create default settings if none exist
                settings = new BonusSettings { Name = "Default", IsActive = true };
                await _uow.BonusSettings.AddAsync(settings);
                await _uow.CompleteAsync();
            }

            return new Response<BonusSettingsDto>(HttpStatusCode.OK, MapToDto(settings));
        }
        catch (Exception ex)
        {
            return new Response<BonusSettingsDto>(HttpStatusCode.InternalServerError, 
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<BonusSettingsDto>> UpdateSettingsAsync(UpdateBonusSettingsDto dto)
    {
        try
        {
            var settings = await _uow.BonusSettings.GetActiveAsync();
            if (settings == null)
            {
                return new Response<BonusSettingsDto>(HttpStatusCode.NotFound, 
                    new List<string> { "Settings not found" });
            }

            // Update only provided fields
            if (dto.QuantityPremiumVehicleRate.HasValue)
                settings.QuantityPremiumVehicleRate = dto.QuantityPremiumVehicleRate.Value;
            if (dto.QuantityStandardVehicleRate.HasValue)
                settings.QuantityStandardVehicleRate = dto.QuantityStandardVehicleRate.Value;
            if (dto.QuantityFromAirportPremiumRate.HasValue)
                settings.QuantityFromAirportPremiumRate = dto.QuantityFromAirportPremiumRate.Value;
            if (dto.QuantityFromAirportStandardRate.HasValue)
                settings.QuantityFromAirportStandardRate = dto.QuantityFromAirportStandardRate.Value;
            if (dto.QuantityFromRailwayPremiumRate.HasValue)
                settings.QuantityFromRailwayPremiumRate = dto.QuantityFromRailwayPremiumRate.Value;
            if (dto.QuantityFromRailwayStandardRate.HasValue)
                settings.QuantityFromRailwayStandardRate = dto.QuantityFromRailwayStandardRate.Value;
            if (dto.RoundTripPremiumVehicleRate.HasValue)
                settings.RoundTripPremiumVehicleRate = dto.RoundTripPremiumVehicleRate.Value;
            if (dto.RoundTripStandardVehicleRate.HasValue)
                settings.RoundTripStandardVehicleRate = dto.RoundTripStandardVehicleRate.Value;
            if (dto.DurationUnder2HoursRate.HasValue)
                settings.DurationUnder2HoursRate = dto.DurationUnder2HoursRate.Value;
            if (dto.DurationUnder4HoursRate.HasValue)
                settings.DurationUnder4HoursRate = dto.DurationUnder4HoursRate.Value;
            if (dto.Duration4To6HoursRate.HasValue)
                settings.Duration4To6HoursRate = dto.Duration4To6HoursRate.Value;
            if (dto.Duration6To8HoursRate.HasValue)
                settings.Duration6To8HoursRate = dto.Duration6To8HoursRate.Value;
            if (dto.Duration8To10HoursRate.HasValue)
                settings.Duration8To10HoursRate = dto.Duration8To10HoursRate.Value;
            if (dto.Duration10To12HoursRate.HasValue)
                settings.Duration10To12HoursRate = dto.Duration10To12HoursRate.Value;
            if (dto.Duration12To14HoursRate.HasValue)
                settings.Duration12To14HoursRate = dto.Duration12To14HoursRate.Value;
            if (dto.DurationOver14HoursRate.HasValue)
                settings.DurationOver14HoursRate = dto.DurationOver14HoursRate.Value;
            if (dto.FieldTripDailyRate.HasValue)
                settings.FieldTripDailyRate = dto.FieldTripDailyRate.Value;
            if (dto.PremiumVehicleTypes != null)
                settings.PremiumVehicleTypes = dto.PremiumVehicleTypes;

            settings.UpdatedAt = DateTime.UtcNow;
            _uow.BonusSettings.Update(settings);
            await _uow.CompleteAsync();

            return new Response<BonusSettingsDto>(HttpStatusCode.OK, "Settings updated", MapToDto(settings));
        }
        catch (Exception ex)
        {
            return new Response<BonusSettingsDto>(HttpStatusCode.InternalServerError, 
                new List<string> { ex.Message });
        }
    }
    public async Task<Response<List<ServiceTypeBonusConfigDto>>> GetServiceTypeConfigsAsync()
    {
        try
        {
            var configs = await _uow.ServiceTypeBonusConfigs.GetAllWithServiceTypeAsync();
            var serviceTypes = await _uow.ServiceTypes.GetAllAsync();

            // Ensure all service types have a config
            var configuredServiceTypeIds = configs.Select(c => c.ServiceTypeId).ToHashSet();
            var missingServiceTypes = serviceTypes.Where(st => !configuredServiceTypeIds.Contains(st.Id)).ToList();

            if (missingServiceTypes.Any())
            {
                var newConfigs = missingServiceTypes.Select(st => new ServiceTypeBonusConfig
                {
                    ServiceTypeId = st.Id,
                    CalculationMethod = GetDefaultCalculationMethod(st.Name)
                }).ToList();

                await _uow.ServiceTypeBonusConfigs.AddRangeAsync(newConfigs);
                await _uow.CompleteAsync();

                configs = await _uow.ServiceTypeBonusConfigs.GetAllWithServiceTypeAsync();
            }

            var dtos = configs.Select(c => new ServiceTypeBonusConfigDto
            {
                Id = c.Id,
                ServiceTypeId = c.ServiceTypeId,
                ServiceTypeName = c.ServiceType?.Name ?? "Unknown",
                CalculationMethod = c.CalculationMethod
            }).ToList();

            return new Response<List<ServiceTypeBonusConfigDto>>(HttpStatusCode.OK, dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<ServiceTypeBonusConfigDto>>(HttpStatusCode.InternalServerError, 
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<ServiceTypeBonusConfigDto>> UpdateServiceTypeConfigAsync(UpdateServiceTypeBonusConfigDto dto)
    {
        try
        {
            var config = await _uow.ServiceTypeBonusConfigs.GetByServiceTypeIdAsync(dto.ServiceTypeId);

            if (config == null)
            {
                config = new ServiceTypeBonusConfig
                {
                    ServiceTypeId = dto.ServiceTypeId,
                    CalculationMethod = dto.CalculationMethod
                };
                await _uow.ServiceTypeBonusConfigs.AddAsync(config);
            }
            else
            {
                config.CalculationMethod = dto.CalculationMethod;
                _uow.ServiceTypeBonusConfigs.Update(config);
            }

            await _uow.CompleteAsync();

            var updated = await _uow.ServiceTypeBonusConfigs.GetByServiceTypeIdAsync(dto.ServiceTypeId);

            return new Response<ServiceTypeBonusConfigDto>(HttpStatusCode.OK, "Config updated", 
                new ServiceTypeBonusConfigDto
                {
                    Id = updated!.Id,
                    ServiceTypeId = updated.ServiceTypeId,
                    ServiceTypeName = updated.ServiceType?.Name ?? "Unknown",
                    CalculationMethod = updated.CalculationMethod
                });
        }
        catch (Exception ex)
        {
            return new Response<ServiceTypeBonusConfigDto>(HttpStatusCode.InternalServerError, 
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<string>> InitializeDefaultConfigsAsync()
    {
        try
        {
            var serviceTypes = await _uow.ServiceTypes.GetAllAsync();
            var existingConfigs = await _uow.ServiceTypeBonusConfigs.GetAllWithServiceTypeAsync();
            var existingIds = existingConfigs.Select(c => c.ServiceTypeId).ToHashSet();

            var newConfigs = new List<ServiceTypeBonusConfig>();

            foreach (var st in serviceTypes)
            {
                if (!existingIds.Contains(st.Id))
                {
                    newConfigs.Add(new ServiceTypeBonusConfig
                    {
                        ServiceTypeId = st.Id,
                        CalculationMethod = GetDefaultCalculationMethod(st.Name)
                    });
                }
            }

            if (newConfigs.Any())
            {
                await _uow.ServiceTypeBonusConfigs.AddRangeAsync(newConfigs);
                await _uow.CompleteAsync();
            }

            return new Response<string>(HttpStatusCode.OK, $"Initialized {newConfigs.Count} new configs");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, 
                new List<string> { ex.Message });
        }
    }

    private static BonusCalculationMethod GetDefaultCalculationMethod(string serviceTypeName)
    {
        var name = serviceTypeName.ToLowerInvariant();

        if (name.Contains("field trip"))
            return BonusCalculationMethod.FieldTripBased;
        if (name.Contains("round trip"))
            return BonusCalculationMethod.RoundTripBased;
        if (name.Contains("itinerary") || name.Contains("customer"))
            return BonusCalculationMethod.DurationBased;

        // Default for Transfer, Airport, Railway, etc.
        return BonusCalculationMethod.QuantityBased;
    }

    private static BonusSettingsDto MapToDto(BonusSettings settings) => new()
    {
        Id = settings.Id,
        Name = settings.Name,
        IsActive = settings.IsActive,
        QuantityPremiumVehicleRate = settings.QuantityPremiumVehicleRate,
        QuantityStandardVehicleRate = settings.QuantityStandardVehicleRate,
        QuantityFromAirportPremiumRate = settings.QuantityFromAirportPremiumRate,
        QuantityFromAirportStandardRate = settings.QuantityFromAirportStandardRate,
        QuantityFromRailwayPremiumRate = settings.QuantityFromRailwayPremiumRate,
        QuantityFromRailwayStandardRate = settings.QuantityFromRailwayStandardRate,
        RoundTripPremiumVehicleRate = settings.RoundTripPremiumVehicleRate,
        RoundTripStandardVehicleRate = settings.RoundTripStandardVehicleRate,
        DurationUnder2HoursRate = settings.DurationUnder2HoursRate,
        DurationUnder4HoursRate = settings.DurationUnder4HoursRate,
        Duration4To6HoursRate = settings.Duration4To6HoursRate,
        Duration6To8HoursRate = settings.Duration6To8HoursRate,
        Duration8To10HoursRate = settings.Duration8To10HoursRate,
        Duration10To12HoursRate = settings.Duration10To12HoursRate,
        Duration12To14HoursRate = settings.Duration12To14HoursRate,
        DurationOver14HoursRate = settings.DurationOver14HoursRate,
        FieldTripDailyRate = settings.FieldTripDailyRate,
        PremiumVehicleTypes = settings.PremiumVehicleTypes
    };
}