using Clean.Application.Dtos.Company;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface ICompanyService
{
    // Categories
    Task<Response<List<CompanyCategoryDto>>> GetAllCategoriesAsync();
    Task<Response<CompanyCategoryDto>> CreateCategoryAsync(CreateCompanyCategoryDto dto);
    Task<Response<CompanyCategoryDto>> UpdateCategoryAsync(int id, UpdateCompanyCategoryDto dto);
    Task<Response<string>> DeleteCategoryAsync(int id);
    
    // Companies
    Task<Response<List<CompanyDto>>> GetAllCompaniesAsync();
    Task<Response<List<CompanyDto>>> GetUncategorizedCompaniesAsync();
    Task<Response<CompanyDto>> UpdateCompanyAsync(int id, UpdateCompanyDto dto);
    Task<Response<string>> BulkAssignCategoryAsync(BulkAssignCategoryDto dto);
    Task<Response<string>> SyncCompaniesFromTransactionsAsync();
}