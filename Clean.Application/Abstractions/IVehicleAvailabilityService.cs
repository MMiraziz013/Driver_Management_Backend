using Clean.Application.Dtos.Vehicle;

namespace Clean.Application.Abstractions;

public interface IVehicleAvailabilityService
{
    Task<List<VehicleUnavailablePeriodDto>> GetByVehicleIdAsync(int vehicleId);
    Task<List<VehicleUnavailablePeriodDto>> GetAllAsync();
    Task<VehicleUnavailablePeriodDto?> CreateAsync(CreateVehicleUnavailablePeriodDto dto);
    Task<VehicleUnavailablePeriodDto?> UpdateAsync(UpdateVehicleUnavailablePeriodDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> IsVehicleAvailableOnDateAsync(int vehicleId, DateTime date);
    Task<List<int>> GetUnavailableVehicleIdsForDateAsync(DateTime date);
    Task<List<int>> GetUnavailableVehicleIdsForPeriodAsync(DateTime startDate, DateTime endDate);
}