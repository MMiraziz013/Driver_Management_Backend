using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IVehicleRepository
{
    Task<List<Vehicle>> GetAllAsync();
    
    Task<List<Vehicle>> GetActiveAndInactiveAsync();
    Task<Vehicle?> GetByIdAsync(int id); // Add this
    Task AddAsync(Vehicle vehicle);
    Task<Vehicle?> Update(Vehicle vehicle);
    Task<bool> Delete(int id);

    Task<bool> ChangeStatus(int id);
}