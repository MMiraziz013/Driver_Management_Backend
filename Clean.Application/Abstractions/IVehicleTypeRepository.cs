using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IVehicleTypeRepository
{
    IQueryable<VehicleType> Query();

    Task<List<VehicleType>> GetAllAsync();
    Task<VehicleType?> GetByIdAsync(int id); // Added
    Task AddAsync(VehicleType vehicleType);  // Added
}