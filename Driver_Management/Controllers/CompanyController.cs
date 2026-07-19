using Clean.Application.Abstractions;
using Clean.Application.Dtos.Company;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/companies")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    // ============ CATEGORIES ============

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _companyService.GetAllCategoriesAsync();
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCompanyCategoryDto dto)
    {
        var result = await _companyService.CreateCategoryAsync(dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCompanyCategoryDto dto)
    {
        var result = await _companyService.UpdateCategoryAsync(id, dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _companyService.DeleteCategoryAsync(id);
        return StatusCode((int)result.StatusCode, result);
    }

    // ============ COMPANIES ============

    [HttpGet]
    public async Task<IActionResult> GetAllCompanies()
    {
        var result = await _companyService.GetAllCompaniesAsync();
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("uncategorized")]
    public async Task<IActionResult> GetUncategorizedCompanies()
    {
        var result = await _companyService.GetUncategorizedCompaniesAsync();
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCompany(int id, [FromBody] UpdateCompanyDto dto)
    {
        var result = await _companyService.UpdateCompanyAsync(id, dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("bulk-assign")]
    public async Task<IActionResult> BulkAssignCategory([FromBody] BulkAssignCategoryDto dto)
    {
        var result = await _companyService.BulkAssignCategoryAsync(dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncFromTransactions()
    {
        var result = await _companyService.SyncCompaniesFromTransactionsAsync();
        return StatusCode((int)result.StatusCode, result);
    }
}