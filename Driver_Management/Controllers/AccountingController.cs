using Clean.Application.Abstractions;
using Clean.Application.Dtos.Accounting;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/accounting")]
public class AccountingController : ControllerBase
{
    private readonly IAccountingUploadService _uploadService;
    private readonly IAnalysisReportService _analysisService;
    private readonly ICarRevenueReportService _carRevenueService;
    private readonly IFarmOutReportService _farmOutService;
    private readonly ICompanyRevenueReportService _companyRevenueService;

    public AccountingController(
        IAccountingUploadService uploadService,
        IAnalysisReportService analysisService,
        ICarRevenueReportService carRevenueService,
        IFarmOutReportService  farmOutService,
        ICompanyRevenueReportService companyRevenueService)
    {
        _uploadService = uploadService;
        _analysisService = analysisService;
        _carRevenueService = carRevenueService;
        _farmOutService = farmOutService;
        _companyRevenueService = companyRevenueService;
    }

    // === UPLOAD ENDPOINTS ===

    [HttpPost("upload/{year}/{month}")]
    public async Task<IActionResult> UploadReport(IFormFile file, int year, int month)
    {
        var userId = User?.Identity?.Name;
        var result = await _uploadService.UploadReportAsync(file, year, month, userId);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetAllReports()
    {
        var result = await _uploadService.GetAllReportsAsync();
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("reports/{year}")]
    public async Task<IActionResult> GetReportsByYear(int year)
    {
        var result = await _uploadService.GetReportsByYearAsync(year);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpDelete("reports/{year}/{month}")]
    public async Task<IActionResult> DeleteReport(int year, int month)
    {
        var result = await _uploadService.DeleteReportAsync(year, month);
        return StatusCode((int)result.StatusCode, result);
    }

    // === ANALYSIS REPORT ENDPOINTS ===

    [HttpPost("analysis")]
    public async Task<IActionResult> GenerateAnalysisReport([FromBody] AnalysisReportRequestDto request)
    {
        var result = await _analysisService.GenerateReportAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("analysis/export")]
    public async Task<IActionResult> ExportAnalysisReport([FromBody] AnalysisReportRequestDto request)
    {
        var result = await _analysisService.ExportToExcelAsync(request);

        if (result.StatusCode != 200 || result.Data == null)
        {
            return StatusCode((int)result.StatusCode, result);
        }

        var years = string.Join("-", request.Years.OrderBy(y => y));
        var fileName = $"Analysis_Report_{years}_{DateTime.Now:yyyyMMdd}.xlsx";

        return File(
            result.Data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
    
    [HttpPost("car-revenue")]
    public async Task<IActionResult> GenerateCarRevenueReport([FromBody] CarRevenueReportRequestDto request)
    {
        var result = await _carRevenueService.GenerateReportAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("car-revenue/export")]
    public async Task<IActionResult> ExportCarRevenueReport([FromBody] CarRevenueReportRequestDto request)
    {
        var result = await _carRevenueService.ExportToExcelAsync(request);

        if (result.StatusCode != 200 || result.Data == null)
        {
            return StatusCode((int)result.StatusCode, result);
        }

        var months = string.Join("-", request.Months.OrderBy(m => m));
        var fileName = $"Car_Revenue_{request.Year}_{months}_{DateTime.Now:yyyyMMdd}.xlsx";

        return File(
            result.Data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
    
    // === FARM OUT REPORT ENDPOINTS ===

    [HttpPost("farm-out")]
    public async Task<IActionResult> GenerateFarmOutReport([FromBody] FarmOutReportRequestDto request)
    {
        var result = await _farmOutService.GenerateReportAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("farm-out/export")]
    public async Task<IActionResult> ExportFarmOutReport([FromBody] FarmOutReportRequestDto request)
    {
        var result = await _farmOutService.ExportToExcelAsync(request);

        if (result.StatusCode != 200 || result.Data == null)
        {
            return StatusCode((int)result.StatusCode, result);
        }

        var months = string.Join("-", request.Months.OrderBy(m => m));
        var fileName = $"Farm_Out_{request.Year}_{months}_{DateTime.Now:yyyyMMdd}.xlsx";

        return File(
            result.Data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
    
    
    // === COMPANY REVENUE REPORT ENDPOINTS ===

    [HttpPost("company-revenue")]
    public async Task<IActionResult> GenerateCompanyRevenueReport([FromBody] CompanyRevenueReportRequestDto request)
    {
        var result = await _companyRevenueService.GenerateReportAsync(request);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpPost("company-revenue/export")]
    public async Task<IActionResult> ExportCompanyRevenueReport([FromBody] CompanyRevenueReportRequestDto request)
    {
        var result = await _companyRevenueService.ExportToExcelAsync(request);

        if (result.StatusCode != 200 || result.Data == null)
        {
            return StatusCode((int)result.StatusCode, result);
        }

        var months = string.Join("-", request.Months.OrderBy(m => m));
        var fileName = $"Company_Revenue_{request.Year}_{months}_{DateTime.Now:yyyyMMdd}.xlsx";

        return File(
            result.Data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }


}