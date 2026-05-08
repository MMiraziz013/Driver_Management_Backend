using System.Globalization;
using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Accounting;
using Clean.Application.Dtos.Responses;
using ClosedXML.Excel;

namespace Clean.Application.Services.Accounting;

public class AnalysisReportService : IAnalysisReportService
{
    private readonly IUnitOfWork _uow;

    public AnalysisReportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<AnalysisReportDto>> GenerateReportAsync(AnalysisReportRequestDto request)
    {
        try
        {
            if (request.Years == null || !request.Years.Any())
            {
                return new Response<AnalysisReportDto>(HttpStatusCode.BadRequest,
                    new List<string> { "At least one year must be selected" });
            }

            var years = request.Years.OrderBy(y => y).ToList();
            
            // If no months specified, include all 12 months
            var selectedMonths = (request.Months != null && request.Months.Any())
                ? request.Months.OrderBy(m => m).ToList()
                : Enumerable.Range(1, 12).ToList();

            // Get all transactions for the requested years
            var allTransactions = await _uow.AccountingTransactions.GetByYearsAsync(years);
            
            // Filter by selected months
            var transactions = allTransactions
                .Where(t => selectedMonths.Contains(t.Month))
                .ToList();

            // Get exchange rates
            var exchangeRates = new Dictionary<int, decimal>();
            foreach (var year in years)
            {
                var rate = await _uow.ExchangeRates.GetByYearAsync(year);
                exchangeRates[year] = rate?.Rate ?? 12500m;
            }

            // Build monthly data (only for selected months)
            var monthlyData = new List<AnalysisMonthRowDto>();
            foreach (var month in selectedMonths)
            {
                var row = new AnalysisMonthRowDto
                {
                    Month = month,
                    MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                    AmountsByYear = new Dictionary<int, decimal>()
                };

                foreach (var year in years)
                {
                    var monthTotal = transactions
                        .Where(t => t.Year == year && t.Month == month)
                        .Sum(t => t.TripTotal);

                    row.AmountsByYear[year] = monthTotal;
                }

                monthlyData.Add(row);
            }

            // Build totals - amounts are in UZS, calculate USD
            var totals = new AnalysisTotalsDto
            {
                TotalUsdByYear = new Dictionary<int, decimal>(),
                TotalUzsByYear = new Dictionary<int, decimal>()
            };

            foreach (var year in years)
            {
                var yearTotalUzs = transactions
                    .Where(t => t.Year == year)
                    .Sum(t => t.TripTotal);

                totals.TotalUzsByYear[year] = yearTotalUzs;

                var rate = exchangeRates.GetValueOrDefault(year, 12500m);
                totals.TotalUsdByYear[year] = rate > 0 ? Math.Round(yearTotalUzs / rate, 2) : 0;
            }

            // Build ALL year-to-year comparisons
            var comparisons = new List<AnalysisYearComparisonDto>();

            for (int i = 0; i < years.Count; i++)
            {
                for (int j = i + 1; j < years.Count; j++)
                {
                    var baseYear = years[i];
                    var compareYear = years[j];

                    var comparison = new AnalysisYearComparisonDto
                    {
                        BaseYear = baseYear,
                        CompareYear = compareYear,
                        MonthlyPercentageChange = new Dictionary<int, decimal>()
                    };

                    foreach (var month in selectedMonths)
                    {
                        var monthData = monthlyData.FirstOrDefault(m => m.Month == month);
                        var baseAmount = monthData?.AmountsByYear.GetValueOrDefault(baseYear, 0) ?? 0;
                        var compareAmount = monthData?.AmountsByYear.GetValueOrDefault(compareYear, 0) ?? 0;

                        decimal percentChange = 0;
                        if (baseAmount != 0)
                        {
                            percentChange = Math.Round(((compareAmount - baseAmount) / baseAmount) * 100, 2);
                        }
                        else if (compareAmount > 0)
                        {
                            percentChange = 100;
                        }

                        comparison.MonthlyPercentageChange[month] = percentChange;
                    }

                    // Total percentage change
                    var baseTotal = totals.TotalUzsByYear.GetValueOrDefault(baseYear, 0);
                    var compareTotal = totals.TotalUzsByYear.GetValueOrDefault(compareYear, 0);

                    if (baseTotal != 0)
                    {
                        comparison.TotalPercentageChange = Math.Round(((compareTotal - baseTotal) / baseTotal) * 100, 2);
                    }
                    else if (compareTotal > 0)
                    {
                        comparison.TotalPercentageChange = 100;
                    }

                    comparisons.Add(comparison);
                }
            }

            var result = new AnalysisReportDto
            {
                Years = years,
                Months = selectedMonths,
                MonthlyData = monthlyData,
                Totals = totals,
                YearComparisons = comparisons,
                ExchangeRates = exchangeRates,
                GeneratedAt = DateTime.UtcNow
            };

            return new Response<AnalysisReportDto>(HttpStatusCode.OK, result);
        }
        catch (Exception ex)
        {
            return new Response<AnalysisReportDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message, ex.StackTrace ?? "" });
        }
    }
    public async Task<Response<byte[]>> ExportToExcelAsync(AnalysisReportRequestDto request)
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
            var ws = workbook.Worksheets.Add("Analysis");

            int col = 1;
            int row = 1;

            // Title
            ws.Cell(row, 1).Value = "REVENUE ANALYSIS REPORT";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 16;
            ws.Range(row, 1, row, data.Years.Count + 1).Merge();
            row += 2;

            // Exchange rates info
            ws.Cell(row, 1).Value = "Exchange Rates (USD to UZS):";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;
            foreach (var year in data.Years)
            {
                ws.Cell(row, 1).Value = $"{year}:";
                ws.Cell(row, 2).Value = data.ExchangeRates.GetValueOrDefault(year, 0);
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }
            row += 2;

            // === MONTHLY REVENUE TABLE ===
            ws.Cell(row, 1).Value = "MONTHLY REVENUE (UZS)";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Range(row, 1, row, data.Years.Count + 1).Merge();
            ws.Range(row, 1, row, data.Years.Count + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            row++;

            // Headers
            ws.Cell(row, 1).Value = "Month";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

            col = 2;
            foreach (var year in data.Years)
            {
                ws.Cell(row, col).Value = year;
                ws.Cell(row, col).Style.Font.Bold = true;
                ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                col++;
            }
            row++;

            // Monthly data
            foreach (var monthData in data.MonthlyData)
            {
                ws.Cell(row, 1).Value = monthData.MonthName;
                col = 2;
                foreach (var year in data.Years)
                {
                    var amount = monthData.AmountsByYear.GetValueOrDefault(year, 0);
                    ws.Cell(row, col).Value = amount;
                    ws.Cell(row, col).Style.NumberFormat.Format = "#,##0";  // No decimals for UZS
                    col++;
                }
                row++;
            }

            // Totals row (USD)
            ws.Cell(row, 1).Value = "TOTAL (USD)";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
            col = 2;
            foreach (var year in data.Years)
            {
                var total = data.Totals.TotalUsdByYear.GetValueOrDefault(year, 0);
                ws.Cell(row, col).Value = total;
                ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, col).Style.Font.Bold = true;
                ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightYellow;
                col++;
            }
            row++;

            // Totals row (UZS)
            ws.Cell(row, 1).Value = "TOTAL (UZS)";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
            col = 2;
            foreach (var year in data.Years)
            {
                var total = data.Totals.TotalUzsByYear.GetValueOrDefault(year, 0);
                ws.Cell(row, col).Value = total;
                ws.Cell(row, col).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, col).Style.Font.Bold = true;
                ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightGreen;
                col++;
            }
            row += 3;

            // === YEAR-OVER-YEAR COMPARISON ===
            // === YEAR-OVER-YEAR COMPARISON ===
            if (data.YearComparisons.Any())
            {
                ws.Cell(row, 1).Value = "YEAR-OVER-YEAR COMPARISON (% Change)";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Range(row, 1, row, data.YearComparisons.Count + 1).Merge();
                ws.Range(row, 1, row, data.YearComparisons.Count + 1).Style.Fill.BackgroundColor = XLColor.LightCoral;
                row++;

                // Headers - show all comparison pairs
                ws.Cell(row, 1).Value = "Month";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

                col = 2;
                foreach (var comparison in data.YearComparisons)
                {
                    ws.Cell(row, col).Value = $"{comparison.BaseYear} vs {comparison.CompareYear}";
                    ws.Cell(row, col).Style.Font.Bold = true;
                    ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightGray;
                    ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    col++;
                }
                row++;

                // Monthly comparisons (only selected months)
                foreach (var month in data.Months)
                {
                    var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
                    ws.Cell(row, 1).Value = monthName;
                    col = 2;
                    foreach (var comparison in data.YearComparisons)
                    {
                        var percentChange = comparison.MonthlyPercentageChange.GetValueOrDefault(month, 0);
                        ws.Cell(row, col).Value = percentChange / 100;
                        ws.Cell(row, col).Style.NumberFormat.Format = "+0.00%;-0.00%;0%";

                        if (percentChange > 0)
                            ws.Cell(row, col).Style.Font.FontColor = XLColor.Green;
                        else if (percentChange < 0)
                            ws.Cell(row, col).Style.Font.FontColor = XLColor.Red;

                        col++;
                    }
                    row++;
                }

                // Total comparison row
                ws.Cell(row, 1).Value = "TOTAL CHANGE";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
                col = 2;
                foreach (var comparison in data.YearComparisons)
                {
                    var percentChange = comparison.TotalPercentageChange;
                    ws.Cell(row, col).Value = percentChange / 100; // Excel percentage format
                    ws.Cell(row, col).Style.NumberFormat.Format = "+0.00%;-0.00%;0%";
                    ws.Cell(row, col).Style.Font.Bold = true;
                    ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightYellow;

                    if (percentChange > 0)
                        ws.Cell(row, col).Style.Font.FontColor = XLColor.Green;
                    else if (percentChange < 0)
                        ws.Cell(row, col).Style.Font.FontColor = XLColor.Red;

                    col++;
                }
            }
            // Auto-fit columns
            ws.Columns().AdjustToContents();

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
}