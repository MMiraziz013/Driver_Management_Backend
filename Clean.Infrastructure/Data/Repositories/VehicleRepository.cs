using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly DataContext _context;

    public VehicleRepository(DataContext context)
    {
        _context = context;
    }
    
    public async Task<List<Vehicle>> GetAllAsync()
    {
        return await _context.Vehicles
            .Include(v => v.Assignments)
            .ThenInclude(a => a.Trip)
            .Include(v => v.VehicleType) // Added this to ensure type names are available
            .Where(v=> v.IsActive)
            .ToListAsync();
    }
    
    public async Task<Vehicle?> GetByIdAsync(int id)
    {
        return await _context.Vehicles
            .Include(v => v.VehicleType)
            .Include(v => v.Assignments)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task AddAsync(Vehicle vehicle)
    {
        await _context.Vehicles.AddAsync(vehicle);
    }

    public async Task<Vehicle?> Update(Vehicle vehicle)
    {
        var toUpdate = await GetByIdAsync(vehicle.Id);
        if (toUpdate is null)
        {
            return null;
        }

        // Manually update properties instead of SetValues
        toUpdate.PlateNumber = vehicle.PlateNumber;
        toUpdate.Model = vehicle.Model;
        toUpdate.Color = vehicle.Color;
        toUpdate.VehicleTypeId = vehicle.VehicleTypeId;
        toUpdate.RequiredDriverCategory = vehicle.RequiredDriverCategory;
        toUpdate.UpdatedAt = DateTime.UtcNow;

        var updated = await _context.SaveChangesAsync();

        if (updated > 0)
        {
            // Reload with navigation properties
            await _context.Entry(toUpdate).Reference(v => v.VehicleType).LoadAsync();
            return toUpdate;
        }

        return null;
    }

    public async Task<bool> Delete(int id)
    {
        var existing = await _context.Vehicles.FindAsync(id);
        if (existing == null)
        {
            return false;
        }

        _context.Vehicles.Remove(existing);
        var result = await _context.SaveChangesAsync(); 
        return result > 0;
    }

    public async Task<bool> ChangeStatus(int id)
    {
        var existing = await _context.Vehicles.FindAsync(id);
        if (existing == null)
        {
            return false;
        }

        if (existing.IsActive)
        {
            existing.IsActive = false;
        }
        else
        {
            existing.IsActive = true;
        }

        var result = await _context.SaveChangesAsync();
        return result > 0;
    }
}