using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IVehicleRepository
{
    Task<List<Vehicle>> GetAllAsync();
    Task<Vehicle?> GetByIdAsync(int id); // Add this
    Task AddAsync(Vehicle vehicle);
    Task<Vehicle?> Update(Vehicle vehicle);
    Task<bool> Delete(int id);
}