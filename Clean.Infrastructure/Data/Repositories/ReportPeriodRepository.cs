using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class ReportPeriodRepository : IReportPeriodRepository
{
    private readonly DataContext _context;

    public ReportPeriodRepository(DataContext context)
    {
        _context = context;
    }

    // Standard CRUD Implementations
    public async Task<ReportPeriod?> GetByIdAsync(int id)
    {
        return await _context.ReportPeriods.FindAsync(id);
    }

    public async Task<List<ReportPeriod>> GetAllAsync()
    {
        return await _context.ReportPeriods
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
    }

    public async Task AddAsync(ReportPeriod period)
    {
        await _context.ReportPeriods.AddAsync(period);
    }

    public void Update(ReportPeriod period)
    {
        _context.ReportPeriods.Update(period);
    }

    public void Delete(ReportPeriod period)
    {
        _context.ReportPeriods.Remove(period);
    }

    // Specialized Methods for Logic & Export
    public async Task<ReportPeriod?> GetWithTripsAsync(int id)
    {
        return await _context.ReportPeriods
            .Include(p => p.Trips)
            .ThenInclude(t => t.VehicleType)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<ReportPeriod?> GetWithAssignmentsAsync(int id)
    {
        return await _context.ReportPeriods
            .Include(p => p.Trips)
                .ThenInclude(t => t.Assignments)
                    .ThenInclude(a => a.Driver)
            .Include(p => p.Trips)
                .ThenInclude(t => t.Assignments)
                    .ThenInclude(a => a.Vehicle)
            .Include(p=> p.Trips)
                .ThenInclude(t=> t.ServiceType)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}