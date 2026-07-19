using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class CompanyCategoryRepository : ICompanyCategoryRepository
{
    private readonly DataContext _context;

    public CompanyCategoryRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<List<CompanyCategory>> GetAllAsync()
    {
        return await _context.CompanyCategories
            .Include(c => c.Companies)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<CompanyCategory?> GetByIdAsync(int id)
    {
        return await _context.CompanyCategories
            .Include(c => c.Companies)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CompanyCategory?> GetByNameAsync(string name)
    {
        return await _context.CompanyCategories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
    }

    public async Task AddAsync(CompanyCategory category)
    {
        await _context.CompanyCategories.AddAsync(category);
    }

    public void Update(CompanyCategory category)
    {
        _context.CompanyCategories.Update(category);
    }

    public void Delete(CompanyCategory category)
    {
        _context.CompanyCategories.Remove(category);
    }
}