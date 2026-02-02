using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IVehicleFuelAllocationRepository
{
    Task<VehicleFuelAllocation?> GetByIdAsync(int id);
    Task<List<VehicleFuelAllocation>> GetAllAsync();
    Task<List<VehicleFuelAllocation>> GetByPeriodIdAsync(int periodId);
    Task<List<VehicleFuelAllocation>> GetByVehicleIdAsync(int vehicleId);
    Task<List<VehicleFuelAllocation>> GetByVehicleAndPeriodAsync(int vehicleId, int periodId);
    Task<List<VehicleFuelAllocation>> GetByPurchaseIdAsync(int purchaseId);
    Task AddAsync(VehicleFuelAllocation entity);
    Task AddRangeAsync(IEnumerable<VehicleFuelAllocation> entities);
    void Remove(VehicleFuelAllocation entity);
    void RemoveRange(IEnumerable<VehicleFuelAllocation> entities);
}
