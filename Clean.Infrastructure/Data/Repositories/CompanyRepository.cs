using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly DataContext _context;

    public CompanyRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<List<Company>> GetAllAsync()
    {
        return await _context.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Company>> GetAllWithCategoryAsync()
    {
        return await _context.Companies
            .Include(c => c.Category)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Company?> GetByIdAsync(int id)
    {
        return await _context.Companies
            .Include(c => c.Category)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Company?> GetByNameAsync(string name)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
    }

    public async Task<Company?> GetByNormalizedNameAsync(string normalizedName)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.NormalizedName == normalizedName);
    }

    public async Task<List<Company>> GetByCategoryIdAsync(int categoryId)
    {
        return await _context.Companies
            .Where(c => c.CompanyCategoryId == categoryId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<Company>> GetUncategorizedAsync()
    {
        return await _context.Companies
            .Where(c => c.CompanyCategoryId == null)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task AddAsync(Company company)
    {
        await _context.Companies.AddAsync(company);
    }

    public async Task AddRangeAsync(IEnumerable<Company> companies)
    {
        await _context.Companies.AddRangeAsync(companies);
    }

    public void Update(Company company)
    {
        _context.Companies.Update(company);
    }

    public void Delete(Company company)
    {
        _context.Companies.Remove(company);
    }
}