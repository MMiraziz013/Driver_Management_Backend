using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Company;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;

namespace Clean.Application.Services.Company;

public class CompanyService : ICompanyService
{
    private readonly IUnitOfWork _uow;

    public CompanyService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // ============ CATEGORIES ============

    public async Task<Response<List<CompanyCategoryDto>>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = await _uow.CompanyCategories.GetAllAsync();
            var dtos = categories.Select(c => new CompanyCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                Color = c.Color,
                CompanyCount = c.Companies.Count
            }).ToList();

            return new Response<List<CompanyCategoryDto>>(HttpStatusCode.OK, dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<CompanyCategoryDto>>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<CompanyCategoryDto>> CreateCategoryAsync(CreateCompanyCategoryDto dto)
    {
        try
        {
            var existing = await _uow.CompanyCategories.GetByNameAsync(dto.Name);
            if (existing != null)
            {
                return new Response<CompanyCategoryDto>(HttpStatusCode.BadRequest,
                    new List<string> { "Category with this name already exists" });
            }

            var category = new CompanyCategory
            {
                Name = dto.Name,
                Description = dto.Description,
                DisplayOrder = dto.DisplayOrder,
                Color = dto.Color,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _uow.CompanyCategories.AddAsync(category);
            await _uow.CompleteAsync();

            var result = new CompanyCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                DisplayOrder = category.DisplayOrder,
                Color = category.Color,
                CompanyCount = 0
            };

            return new Response<CompanyCategoryDto>(HttpStatusCode.Created, "Category created", result);
        }
        catch (Exception ex)
        {
            return new Response<CompanyCategoryDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<CompanyCategoryDto>> UpdateCategoryAsync(int id, UpdateCompanyCategoryDto dto)
    {
        try
        {
            var category = await _uow.CompanyCategories.GetByIdAsync(id);
            if (category == null)
            {
                return new Response<CompanyCategoryDto>(HttpStatusCode.NotFound,
                    new List<string> { "Category not found" });
            }

            if (!string.IsNullOrWhiteSpace(dto.Name))
                category.Name = dto.Name;
            if (dto.Description != null)
                category.Description = dto.Description;
            if (dto.DisplayOrder.HasValue)
                category.DisplayOrder = dto.DisplayOrder.Value;
            if (dto.Color != null)
                category.Color = dto.Color;

            category.UpdatedAt = DateTime.UtcNow;
            _uow.CompanyCategories.Update(category);
            await _uow.CompleteAsync();

            var result = new CompanyCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                DisplayOrder = category.DisplayOrder,
                Color = category.Color,
                CompanyCount = category.Companies.Count
            };

            return new Response<CompanyCategoryDto>(HttpStatusCode.OK, "Category updated", result);
        }
        catch (Exception ex)
        {
            return new Response<CompanyCategoryDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<string>> DeleteCategoryAsync(int id)
    {
        try
        {
            var category = await _uow.CompanyCategories.GetByIdAsync(id);
            if (category == null)
            {
                return new Response<string>(HttpStatusCode.NotFound,
                    new List<string> { "Category not found" });
            }

            _uow.CompanyCategories.Delete(category);
            await _uow.CompleteAsync();

            return new Response<string>(HttpStatusCode.OK, "Category deleted");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    // ============ COMPANIES ============

    public async Task<Response<List<CompanyDto>>> GetAllCompaniesAsync()
    {
        try
        {
            var companies = await _uow.Companies.GetAllWithCategoryAsync();
            var dtos = companies.Select(MapToDto).ToList();
            return new Response<List<CompanyDto>>(HttpStatusCode.OK, dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<CompanyDto>>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<List<CompanyDto>>> GetUncategorizedCompaniesAsync()
    {
        try
        {
            var companies = await _uow.Companies.GetUncategorizedAsync();
            var dtos = companies.Select(MapToDto).ToList();
            return new Response<List<CompanyDto>>(HttpStatusCode.OK, dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<CompanyDto>>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<CompanyDto>> UpdateCompanyAsync(int id, UpdateCompanyDto dto)
    {
        try
        {
            var company = await _uow.Companies.GetByIdAsync(id);
            if (company == null)
            {
                return new Response<CompanyDto>(HttpStatusCode.NotFound,
                    new List<string> { "Company not found" });
            }

            if (dto.CompanyCategoryId.HasValue)
                company.CompanyCategoryId = dto.CompanyCategoryId.Value == 0 ? null : dto.CompanyCategoryId;
            if (dto.Aliases != null)
                company.Aliases = dto.Aliases;
            if (dto.Notes != null)
                company.Notes = dto.Notes;
            if (dto.IsActive.HasValue)
                company.IsActive = dto.IsActive.Value;

            company.UpdatedAt = DateTime.UtcNow;
            _uow.Companies.Update(company);
            await _uow.CompleteAsync();

            // Reload with category
            company = await _uow.Companies.GetByIdAsync(id);
            return new Response<CompanyDto>(HttpStatusCode.OK, "Company updated", MapToDto(company!));
        }
        catch (Exception ex)
        {
            return new Response<CompanyDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<string>> BulkAssignCategoryAsync(BulkAssignCategoryDto dto)
    {
        try
        {
            var companies = await _uow.Companies.GetAllAsync();
            var toUpdate = companies.Where(c => dto.CompanyIds.Contains(c.Id)).ToList();

            foreach (var company in toUpdate)
            {
                company.CompanyCategoryId = dto.CategoryId == 0 ? null : dto.CategoryId;
                company.UpdatedAt = DateTime.UtcNow;
                _uow.Companies.Update(company);
            }

            await _uow.CompleteAsync();

            return new Response<string>(HttpStatusCode.OK, $"Updated {toUpdate.Count} companies");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<string>> SyncCompaniesFromTransactionsAsync()
    {
        try
        {
            // Get all unique company names from transactions
            var transactions = await _uow.AccountingTransactions.GetByYearsAsync(
                Enumerable.Range(2020, 10).ToList());

            var companyNames = transactions
                .Where(t => !string.IsNullOrWhiteSpace(t.Company))
                .Select(t => t.Company!.Trim())
                .Distinct()
                .ToList();

            // Get existing companies
            var existingCompanies = await _uow.Companies.GetAllAsync();
            var existingNormalized = existingCompanies
                .Select(c => c.NormalizedName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Find new companies
            var newCompanies = new List<Domain.Entities.Company>();
            foreach (var name in companyNames)
            {
                var normalized = name.ToUpperInvariant();
                if (!existingNormalized.Contains(normalized))
                {
                    newCompanies.Add(new Domain.Entities.Company
                    {
                        Name = name,
                        NormalizedName = normalized,
                        FirstSeenAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    existingNormalized.Add(normalized);
                }
            }

            if (newCompanies.Any())
            {
                await _uow.Companies.AddRangeAsync(newCompanies);
                await _uow.CompleteAsync();
            }

            return new Response<string>(HttpStatusCode.OK, 
                $"Synced companies. Added {newCompanies.Count} new companies. Total: {existingCompanies.Count + newCompanies.Count}");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    private static CompanyDto MapToDto(Domain.Entities.Company company) => new()
    {
        Id = company.Id,
        Name = company.Name,
        CompanyCategoryId = company.CompanyCategoryId,
        CategoryName = company.Category?.Name,
        CategoryColor = company.Category?.Color,
        Aliases = company.Aliases,
        Notes = company.Notes,
        IsActive = company.IsActive,
        FirstSeenAt = company.FirstSeenAt
    };
}