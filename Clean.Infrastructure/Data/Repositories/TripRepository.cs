using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class TripRepository : ITripRepository
{
    private readonly DataContext _context;

    public TripRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<Trip?> GetByIdAsync(int id)
    {
        return await _context.Trips.FindAsync(id);
    }

    public async Task<Trip?> GetWithDetailsAsync(int id)
    {
        return await _context.Trips
            .Include(t => t.VehicleType)
            .Include(t => t.ServiceType)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Trip>> GetByPeriodAsync(int periodId)
    {
        return await _context.Trips
            .Where(t => t.ReportPeriodId == periodId)
            .OrderBy(t => t.PickUpDate)
            .ThenBy(t => t.GarageOutTime)
            .ToListAsync();
    }

    public async Task<List<Trip>> GetByPeriodWithDetailsAsync(int periodId)
    {
        return await _context.Trips
            .Include(t => t.VehicleType)
            .Include(t => t.ServiceType)
            .Where(t => t.ReportPeriodId == periodId)
            .OrderBy(t => t.PickUpDate)
            .ThenBy(t => t.GarageOutTime)
            .ToListAsync();
    }

    public async Task AddAsync(Trip trip)
    {
        await _context.Trips.AddAsync(trip);
    }

    public void Update(Trip trip)
    {
        _context.Trips.Update(trip);
    }

    public void Remove(Trip trip)
    {
        _context.Trips.Remove(trip);
    }

    public async Task<List<Trip>> GetAllAsync()
    {
        return await _context.Trips.ToListAsync();
    }
}