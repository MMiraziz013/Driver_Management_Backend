using System.Globalization;
using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Accounting;
using Clean.Application.Dtos.Responses;
using ClosedXML.Excel;

namespace Clean.Application.Services.Accounting;

public class CompanyRevenueReportService : ICompanyRevenueReportService
{
    private readonly IUnitOfWork _uow;

    public CompanyRevenueReportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<CompanyRevenueReportDto>> GenerateReportAsync(CompanyRevenueReportRequestDto request)
    {
        try
        {
            if (request.Year <= 0)
            {
                return new Response<CompanyRevenueReportDto>(HttpStatusCode.BadRequest,
                    new List<string> { "Year is required" });
            }

            // If no months specified, include all 12 months
            var selectedMonths = (request.Months != null && request.Months.Any())
                ? request.Months.OrderBy(m => m).ToList()
                : Enumerable.Range(1, 12).ToList();

            // Get exchange rate for the year
            var exchangeRateEntity = await _uow.ExchangeRates.GetByYearAsync(request.Year);
            var exchangeRate = exchangeRateEntity?.Rate ?? 12500m;

            // Get all transactions for the year and selected months
            var transactions = await _uow.AccountingTransactions.GetByYearAsync(request.Year);
            var filteredTransactions = transactions
                .Where(t => selectedMonths.Contains(t.Month))
                .ToList();

            // Get all companies with their categories
            var companies = await _uow.Companies.GetAllWithCategoryAsync();
            var companyLookup = companies.ToDictionary(
                c => c.NormalizedName,
                c => c,
                StringComparer.OrdinalIgnoreCase
            );

            // Also create lookup by original name
            foreach (var company in companies)
            {
                companyLookup.TryAdd(company.Name.ToUpperInvariant(), company);
            }

            // Group transactions by company name
            var groupedByCompany = filteredTransactions
                .Where(t => !string.IsNullOrWhiteSpace(t.Company))
                .GroupBy(t => t.Company!.Trim())
                .ToList();

            var grandTotal = filteredTransactions.Sum(t => t.TripTotal);
            var companyRows = new List<CompanyRevenueRowDto>();

            foreach (var companyGroup in groupedByCompany)
            {
                var companyName = companyGroup.Key;
                var companyTransactions = companyGroup.ToList();

                // Find the company entity to get category
                var normalizedName = companyName.ToUpperInvariant();
                companyLookup.TryGetValue(normalizedName, out var companyEntity);

                var categoryName = companyEntity?.Category?.Name ?? "Uncategorized";

                // Calculate totals
                var total = companyTransactions.Sum(t => t.TripTotal);

                // Calculate monthly amounts
                var monthlyAmounts = new Dictionary<int, decimal>();
                foreach (var month in selectedMonths)
                {
                    var monthTotal = companyTransactions
                        .Where(t => t.Month == month)
                        .Sum(t => t.TripTotal);
                    monthlyAmounts[month] = monthTotal;
                }

                // Calculate portion
                var portionPercent = grandTotal > 0
                    ? Math.Round((total / grandTotal) * 100, 2)
                    : 0;

                companyRows.Add(new CompanyRevenueRowDto
                {
                    CompanyName = companyName,
                    CategoryName = categoryName,
                    Total = total,
                    PortionPercent = portionPercent,
                    MonthlyAmounts = monthlyAmounts,
                    TripCount = companyTransactions.Count
                });
            }

            // Sort by total amount descending
            companyRows = companyRows.OrderByDescending(r => r.Total).ToList();

            // Build category analysis
            var categoryAnalysis = companyRows
                .GroupBy(r => r.CategoryName)
                .Select(g => new CategoryRevenueRowDto
                {
                    CategoryName = g.Key,
                    Revenue = g.Sum(r => r.Total),
                    PortionPercent = grandTotal > 0
                        ? Math.Round((g.Sum(r => r.Total) / grandTotal) * 100, 2)
                        : 0,
                    CompanyCount = g.Count()
                })
                .OrderByDescending(c => c.Revenue)
                .ToList();

            // Calculate totals
            var totals = new CompanyRevenueTotalsDto
            {
                Total = companyRows.Sum(r => r.Total),
                TotalTripCount = companyRows.Sum(r => r.TripCount),
                MonthlyAmounts = new Dictionary<int, decimal>()
            };

            foreach (var month in selectedMonths)
            {
                totals.MonthlyAmounts[month] = companyRows.Sum(r => r.MonthlyAmounts.GetValueOrDefault(month, 0));
            }

            // Build month names
            var monthNames = selectedMonths
                .Select(m => CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(m))
                .ToList();

            var result = new CompanyRevenueReportDto
            {
                Year = request.Year,
                Months = selectedMonths,
                MonthNames = monthNames,
                ExchangeRate = exchangeRate,
                CategoryAnalysis = categoryAnalysis,
                CompanyRows = companyRows,
                Totals = totals,
                GeneratedAt = DateTime.UtcNow
            };

            return new Response<CompanyRevenueReportDto>(HttpStatusCode.OK, result);
        }
        catch (Exception ex)
        {
            return new Response<CompanyRevenueReportDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message, ex.StackTrace ?? "" });
        }
    }

    public async Task<Response<byte[]>> ExportToExcelAsync(CompanyRevenueReportRequestDto request)
    {
        try
        {
            var reportResult = await GenerateReportAsync(request);
            if (reportResult.StatusCode != 200 || reportResult.Data == null)
            {
                return new Response<byte[]>(HttpStatusCode.BadRequest,
                    reportResult.Errors ?? new List<string> { "Failed to generate report" });
            }

            var data = reportResult.Data;

            using var workbook = new XLWorkbook();

            // 1. Create Analyze sheet (Category Summary)
            CreateAnalyzeSheet(workbook, data);

            // 2. Create Total sheet (All Companies)
            CreateTotalSheet(workbook, data);

            // 3. Create individual month sheets
            foreach (var month in data.Months)
            {
                var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
                CreateMonthSheet(workbook, monthName, data, month);
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new Response<byte[]>(HttpStatusCode.OK, "Export successful", stream.ToArray());
        }
        catch (Exception ex)
        {
            return new Response<byte[]>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    private void CreateAnalyzeSheet(XLWorkbook workbook, CompanyRevenueReportDto data)
    {
        var ws = workbook.Worksheets.Add("Analyze");
        int row = 1;

        // Title
        ws.Cell(row, 1).Value = $"REVENUE BY COMPANY - CATEGORY ANALYSIS - {data.Year}";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 16;
        row++;

        ws.Cell(row, 1).Value = $"Months: {string.Join(", ", data.MonthNames)}";
        row += 2;

        // Headers
        var headers = new[] { "Category", "Revenue (UZS)", "Portion %" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
            ws.Cell(row, i + 1).Style.Font.Bold = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Cell(row, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        row++;

        // Data rows
        foreach (var catRow in data.CategoryAnalysis)
        {
            ws.Cell(row, 1).Value = catRow.CategoryName;
            ws.Cell(row, 2).Value = catRow.Revenue;
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 3).Value = catRow.PortionPercent / 100;
            ws.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
            row++;
        }

        // Totals row
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = data.Totals.Total;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = 1;
        ws.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
        ws.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.LightYellow;

        ws.Columns().AdjustToContents();
    }

    private void CreateTotalSheet(XLWorkbook workbook, CompanyRevenueReportDto data)
    {
        var ws = workbook.Worksheets.Add("Total");
        int row = 1;
        int col;

        // Title
        ws.Cell(row, 1).Value = $"REVENUE BY COMPANY - {data.Year}";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 16;
        row++;

        ws.Cell(row, 1).Value = $"Months: {string.Join(", ", data.MonthNames)}";
        row += 2;

        // Headers
        var headers = new List<string>
        {
            "Company Name",
            "Category",
            "Total (UZS)",
            "Portion %"
        };

        // Add month headers
        foreach (var month in data.Months)
        {
            headers.Add(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month));
        }

        col = 1;
        foreach (var header in headers)
        {
            ws.Cell(row, col).Value = header;
            ws.Cell(row, col).Style.Font.Bold = true;
            ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            col++;
        }
        row++;

        // Data rows
        foreach (var companyRow in data.CompanyRows)
        {
            col = 1;
            ws.Cell(row, col++).Value = companyRow.CompanyName;
            ws.Cell(row, col++).Value = companyRow.CategoryName;

            ws.Cell(row, col).Value = companyRow.Total;
            ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";

            ws.Cell(row, col).Value = companyRow.PortionPercent / 100;
            ws.Cell(row, col++).Style.NumberFormat.Format = "0.00%";

            // Monthly amounts
            foreach (var month in data.Months)
            {
                var amount = companyRow.MonthlyAmounts.GetValueOrDefault(month, 0);
                ws.Cell(row, col).Value = amount;
                ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";
            }

            row++;
        }

        // Totals row
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Range(row, 1, row, headers.Count).Style.Fill.BackgroundColor = XLColor.LightYellow;
        ws.Range(row, 1, row, headers.Count).Style.Font.Bold = true;

        col = 3;
        ws.Cell(row, col).Value = data.Totals.Total;
        ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";

        ws.Cell(row, col).Value = 1;
        ws.Cell(row, col++).Style.NumberFormat.Format = "0.00%";

        foreach (var month in data.Months)
        {
            var amount = data.Totals.MonthlyAmounts.GetValueOrDefault(month, 0);
            ws.Cell(row, col).Value = amount;
            ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";
        }

        ws.Columns().AdjustToContents();
    }

    private void CreateMonthSheet(XLWorkbook workbook, string sheetName, CompanyRevenueReportDto data, int month)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        int row = 1;

        // Title
        ws.Cell(row, 1).Value = $"{sheetName} {data.Year} - Revenue by Company";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        row += 2;

        // Headers
        var headers = new[] { "Company Name", "Revenue (UZS)", "Portion %" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
            ws.Cell(row, i + 1).Style.Font.Bold = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Cell(row, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        row++;

        // Filter rows that have data for this month
        var monthRows = data.CompanyRows
            .Where(r => r.MonthlyAmounts.GetValueOrDefault(month, 0) > 0)
            .Select(r => new
            {
                r.CompanyName,
                Amount = r.MonthlyAmounts.GetValueOrDefault(month, 0)
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        var monthTotal = monthRows.Sum(r => r.Amount);

        foreach (var companyRow in monthRows)
        {
            var monthPortion = monthTotal > 0 ? (companyRow.Amount / monthTotal) * 100 : 0;

            ws.Cell(row, 1).Value = companyRow.CompanyName;
            ws.Cell(row, 2).Value = companyRow.Amount;
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 3).Value = monthPortion / 100;
            ws.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
            row++;
        }

        // Totals row
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = monthTotal;
        ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = 1;
        ws.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
        ws.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.LightYellow;

        ws.Columns().AdjustToContents();
    }
}