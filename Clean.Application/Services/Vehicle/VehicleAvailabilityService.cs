using Clean.Application.Abstractions;
using Clean.Application.Dtos.Vehicle;
using Clean.Domain.Entities;

namespace Clean.Application.Services.Vehicle;

public class VehicleAvailabilityService : IVehicleAvailabilityService
{
    private readonly IUnitOfWork _uow;

    public VehicleAvailabilityService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<List<VehicleUnavailablePeriodDto>> GetByVehicleIdAsync(int vehicleId)
    {
        var periods = await _uow.VehicleUnavailablePeriods.GetByVehicleIdAsync(vehicleId);
        return periods.Select(MapToDto).ToList();
    }

    public async Task<List<VehicleUnavailablePeriodDto>> GetAllAsync()
    {
        var periods = await _uow.VehicleUnavailablePeriods.GetAllAsync();
        return periods.Select(MapToDto).ToList();
    }

    public async Task<VehicleUnavailablePeriodDto?> CreateAsync(CreateVehicleUnavailablePeriodDto dto)
    {
        // Validate dates
        if (dto.EndDate < dto.StartDate)
        {
            return null;
        }

        // Check for overlapping periods
        var overlapping = await _uow.VehicleUnavailablePeriods.GetOverlappingPeriodsAsync(
            dto.VehicleId, dto.StartDate, dto.EndDate);

        if (overlapping.Any())
        {
            return null; // Or throw exception with details
        }

        var period = new VehicleUnavailablePeriod
        {
            VehicleId = dto.VehicleId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            Reason = dto.Reason?.Trim(),
            Notes = dto.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _uow.VehicleUnavailablePeriods.AddAsync(period);
        await _uow.CompleteAsync();

        return MapToDto(period);
    }

    public async Task<VehicleUnavailablePeriodDto?> UpdateAsync(UpdateVehicleUnavailablePeriodDto dto)
    {
        var period = await _uow.VehicleUnavailablePeriods.GetByIdAsync(dto.Id);
        if (period == null) return null;

        // Validate dates
        if (dto.EndDate < dto.StartDate)
        {
            return null;
        }

        // Check for overlapping periods (excluding this one)
        var overlapping = await _uow.VehicleUnavailablePeriods.GetOverlappingPeriodsAsync(
            period.VehicleId, dto.StartDate, dto.EndDate, dto.Id);

        if (overlapping.Any())
        {
            return null;
        }

        period.StartDate = dto.StartDate.Date;
        period.EndDate = dto.EndDate.Date;
        period.Reason = dto.Reason?.Trim();
        period.Notes = dto.Notes?.Trim();

        _uow.VehicleUnavailablePeriods.Update(period);
        await _uow.CompleteAsync();

        return MapToDto(period);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var period = await _uow.VehicleUnavailablePeriods.GetByIdAsync(id);
        if (period == null) return false;

        _uow.VehicleUnavailablePeriods.Delete(period);
        await _uow.CompleteAsync();
        return true;
    }

    public async Task<bool> IsVehicleAvailableOnDateAsync(int vehicleId, DateTime date)
    {
        var dateOnly = date.Date;
        var periods = await _uow.VehicleUnavailablePeriods.GetByVehicleIdAsync(vehicleId);
        return !periods.Any(p => p.StartDate <= dateOnly && p.EndDate >= dateOnly);
    }

    public async Task<List<int>> GetUnavailableVehicleIdsForDateAsync(DateTime date)
    {
        var activePeriods = await _uow.VehicleUnavailablePeriods.GetActivePeriodsAsync(date);
        return activePeriods.Select(p => p.VehicleId).Distinct().ToList();
    }

    public async Task<List<int>> GetUnavailableVehicleIdsForPeriodAsync(DateTime startDate, DateTime endDate)
    {
        // Get all periods that overlap with the given date range
        var allPeriods = await _uow.VehicleUnavailablePeriods.GetAllAsync();
        
        var unavailableVehicleIds = allPeriods
            .Where(p => p.StartDate <= endDate && p.EndDate >= startDate)
            .Select(p => p.VehicleId)
            .Distinct()
            .ToList();

        return unavailableVehicleIds;
    }

    private static VehicleUnavailablePeriodDto MapToDto(VehicleUnavailablePeriod period)
    {
        return new VehicleUnavailablePeriodDto
        {
            Id = period.Id,
            VehicleId = period.VehicleId,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            Reason = period.Reason,
            Notes = period.Notes,
            CreatedAt = period.CreatedAt
        };
    }
}