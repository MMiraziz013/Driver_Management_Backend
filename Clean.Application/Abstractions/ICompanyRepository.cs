using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface ICompanyRepository
{
    Task<List<Company>> GetAllAsync();
    Task<List<Company>> GetAllWithCategoryAsync();
    Task<Company?> GetByIdAsync(int id);
    Task<Company?> GetByNameAsync(string name);
    Task<Company?> GetByNormalizedNameAsync(string normalizedName);
    Task<List<Company>> GetByCategoryIdAsync(int categoryId);
    Task<List<Company>> GetUncategorizedAsync();
    Task AddAsync(Company company);
    Task AddRangeAsync(IEnumerable<Company> companies);
    void Update(Company company);
    void Delete(Company company);
}