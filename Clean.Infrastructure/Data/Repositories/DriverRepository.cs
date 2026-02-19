using Clean.Application.Abstractions;
using Clean.Application.Dtos.Driver;
using Clean.Application.Dtos.Filters;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly DataContext _context;

    public DriverRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<Driver?> AddDriverAsync(Driver driver)
    {
        var exists = await GetDriverByIdAsync(driver.Id);
        if (exists is not null)
        {
            return null;
        }

        await _context.Drivers.AddAsync(driver);
        await _context.SaveChangesAsync();

        return driver;
    }

    public async Task<Driver?> GetDriverByIdAsync(int id)
    {
        var driver = await _context.Drivers
            .Include(d=> d.Assignments)
            .Include(d=> d.OffDays)
            .Include(d=> d.Vacations)
            .FirstOrDefaultAsync(d=> d.Id == id);
        return driver;
    }

    public async Task<(List<GetDriverDto> drivers, int totalRecords)> GetDriversAsync(PaginationFilter filter)
    {
        var query = _context.Drivers
            .AsQueryable();
        
        var totalRecords = await query.CountAsync();

        query = query
            .OrderBy(e => e.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize);

        var drivers = await query.Select(d => new GetDriverDto
        {
            Id = d.Id,
            FullName = d.FullName,
            Age = d.Age,
            Address = d.Address,
            EmploymentType = d.EmploymentType,
            LicenseCategory = d.Category,
            IsActive = d.IsActive
        }).ToListAsync();

        return (drivers, totalRecords);
    }
    
    public async Task<List<Driver>> GetActiveDriversWithDetailsAsync()
    {
        return await _context.Drivers
            .Where(d => d.IsActive)
            .Include(d => d.Vacations)
            .Include(d => d.OffDays)
            .Include(d => d.Assignments)
            .ThenInclude(a => a.Trip)
            .ToListAsync();
    }

    public async Task<Driver?> UpdateDriverAsync(Driver driver)
    {
        var toUpdate = await GetDriverByIdAsync(driver.Id);

        if (toUpdate is null)
        {
            return null;
        }
        _context.Entry(toUpdate).CurrentValues.SetValues(driver);
        var updated= await _context.SaveChangesAsync();
        
        return updated > 0 ? toUpdate : null;
    }

    public async Task<bool> DeleteDriverAsync(int id)
    {
        var toDelete = await GetDriverByIdAsync(id);
        if (toDelete is null)
        {
            return false;
        }

        _context.Remove(toDelete);
        var isDeleted = await _context.SaveChangesAsync();

        return isDeleted > 0;
    }

    public async Task<bool> ChangeDriverStatusAsync(int id)
    {
        var toDeactivate = await GetDriverByIdAsync(id);
        if (toDeactivate is null)
        {
            return false;
        }

        toDeactivate.IsActive = !toDeactivate.IsActive;
        
        var isChanged = await _context.SaveChangesAsync();

        return isChanged > 0;
    }
}