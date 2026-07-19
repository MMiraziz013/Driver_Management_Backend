using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface ICompanyCategoryRepository
{
    Task<List<CompanyCategory>> GetAllAsync();
    Task<CompanyCategory?> GetByIdAsync(int id);
    Task<CompanyCategory?> GetByNameAsync(string name);
    Task AddAsync(CompanyCategory category);
    void Update(CompanyCategory category);
    void Delete(CompanyCategory category);
}