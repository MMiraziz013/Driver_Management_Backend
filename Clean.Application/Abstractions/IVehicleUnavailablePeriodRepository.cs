using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IVehicleUnavailablePeriodRepository
{
    Task<VehicleUnavailablePeriod?> GetByIdAsync(int id);
    Task<List<VehicleUnavailablePeriod>> GetByVehicleIdAsync(int vehicleId);
    Task<List<VehicleUnavailablePeriod>> GetAllAsync();
    Task<List<VehicleUnavailablePeriod>> GetActivePeriodsAsync(DateTime date);
    Task<List<VehicleUnavailablePeriod>> GetOverlappingPeriodsAsync(int vehicleId, DateTime startDate, DateTime endDate, int? excludeId = null);
    Task AddAsync(VehicleUnavailablePeriod period);
    void Update(VehicleUnavailablePeriod period);
    void Delete(VehicleUnavailablePeriod period);
}