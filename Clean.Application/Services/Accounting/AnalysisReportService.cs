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
            
            // Selected months for totals calculation
            var selectedMonths = (request.Months != null && request.Months.Any())
                ? request.Months.OrderBy(m => m).ToList()
                : Enumerable.Range(1, 12).ToList();

            // All 12 months for display
            var allMonths = Enumerable.Range(1, 12).ToList();

            // Get all transactions for the requested years (ALL months)
            var allTransactions = await _uow.AccountingTransactions.GetByYearsAsync(years);

            // Get exchange rates
            var exchangeRates = new Dictionary<int, decimal>();
            foreach (var year in years)
            {
                var rate = await _uow.ExchangeRates.GetByYearAsync(year);
                exchangeRates[year] = rate?.Rate ?? 12500m;
            }

            // Build monthly data for ALL 12 months
            var monthlyData = new List<AnalysisMonthRowDto>();
            foreach (var month in allMonths)
            {
                var row = new AnalysisMonthRowDto
                {
                    Month = month,
                    MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                    AmountsByYear = new Dictionary<int, decimal>(),
                    IsSelected = selectedMonths.Contains(month)  // Mark if selected
                };

                foreach (var year in years)
                {
                    var monthTotal = allTransactions
                        .Where(t => t.Year == year && t.Month == month)
                        .Sum(t => t.TripTotal);

                    row.AmountsByYear[year] = monthTotal;
                }

                monthlyData.Add(row);
            }

            // Build totals
            var totals = new AnalysisTotalsDto
            {
                TotalUsdByYear = new Dictionary<int, decimal>(),
                TotalUzsByYear = new Dictionary<int, decimal>(),
                FullYearUsdByYear = new Dictionary<int, decimal>(),
                FullYearUzsByYear = new Dictionary<int, decimal>()
            };

            foreach (var year in years)
            {
                // Selected months total (only selected months)
                var selectedMonthsTotalUzs = allTransactions
                    .Where(t => t.Year == year && selectedMonths.Contains(t.Month))
                    .Sum(t => t.TripTotal);

                totals.TotalUzsByYear[year] = selectedMonthsTotalUzs;

                var rate = exchangeRates.GetValueOrDefault(year, 12500m);
                totals.TotalUsdByYear[year] = rate > 0 ? Math.Round(selectedMonthsTotalUzs / rate, 2) : 0;

                // Full year total (all 12 months)
                var fullYearTotalUzs = allTransactions
                    .Where(t => t.Year == year)
                    .Sum(t => t.TripTotal);

                totals.FullYearUzsByYear[year] = fullYearTotalUzs;
                totals.FullYearUsdByYear[year] = rate > 0 ? Math.Round(fullYearTotalUzs / rate, 2) : 0;
            }

            // Build year-to-year comparisons
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

                    // Calculate percentage change for ALL months
                    foreach (var month in allMonths)
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

                    // Selected months percentage change
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

                    // Full year percentage change
                    var baseFullYear = totals.FullYearUzsByYear.GetValueOrDefault(baseYear, 0);
                    var compareFullYear = totals.FullYearUzsByYear.GetValueOrDefault(compareYear, 0);

                    if (baseFullYear != 0)
                    {
                        comparison.FullYearPercentageChange = Math.Round(((compareFullYear - baseFullYear) / baseFullYear) * 100, 2);
                    }
                    else if (compareFullYear > 0)
                    {
                        comparison.FullYearPercentageChange = 100;
                    }

                    comparisons.Add(comparison);
                }
            }

            var result = new AnalysisReportDto
            {
                Years = years,
                Months = selectedMonths,  // Keep track of which months are selected
                MonthlyData = monthlyData,  // Now contains all 12 months
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
            var selectedMonths = data.Months;
            var isPartialYear = selectedMonths.Count < 12;

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

            // Selected months info (if partial)
            if (isPartialYear)
            {
                var selectedMonthNames = selectedMonths
                    .Select(m => CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(m))
                    .ToList();
                ws.Cell(row, 1).Value = $"Selected Months: {string.Join(", ", selectedMonthNames)}";
                ws.Cell(row, 1).Style.Font.Italic = true;
                ws.Cell(row, 1).Style.Font.FontColor = XLColor.DarkBlue;
                ws.Range(row, 1, row, data.Years.Count + 1).Merge();
                row++;
            }
            row++;

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

            // Monthly data - ALL 12 months, highlight selected
            foreach (var monthData in data.MonthlyData)
            {
                var isSelected = selectedMonths.Contains(monthData.Month);
                
                ws.Cell(row, 1).Value = monthData.MonthName;
                if (isSelected)
                {
                    ws.Cell(row, 1).Style.Font.Bold = true;
                }
                else
                {
                    ws.Cell(row, 1).Style.Font.FontColor = XLColor.Gray;
                }
                
                col = 2;
                foreach (var year in data.Years)
                {
                    var amount = monthData.AmountsByYear.GetValueOrDefault(year, 0);
                    ws.Cell(row, col).Value = amount;
                    ws.Cell(row, col).Style.NumberFormat.Format = "#,##0";
                    
                    if (isSelected)
                    {
                        ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightGoldenrodYellow;
                    }
                    else
                    {
                        ws.Cell(row, col).Style.Font.FontColor = XLColor.Gray;
                    }
                    col++;
                }
                row++;
            }

            // Selected months totals
            var selectedMonthsLabel = isPartialYear ? "SELECTED MONTHS TOTAL" : "TOTAL";
            
            // Selected Months Total (USD)
            ws.Cell(row, 1).Value = $"{selectedMonthsLabel} (USD)";
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

            // Selected Months Total (UZS)
            ws.Cell(row, 1).Value = $"{selectedMonthsLabel} (UZS)";
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
            row++;

            // Full Year Totals (always show for context)
            if (isPartialYear)
            {
                row++; // Empty row separator

                // Full Year Total (USD)
                ws.Cell(row, 1).Value = "FULL YEAR TOTAL (USD)";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightSkyBlue;
                col = 2;
                foreach (var year in data.Years)
                {
                    var total = data.Totals.FullYearUsdByYear.GetValueOrDefault(year, 0);
                    ws.Cell(row, col).Value = total;
                    ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, col).Style.Font.Bold = true;
                    ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightSkyBlue;
                    col++;
                }
                row++;

                // Full Year Total (UZS)
                ws.Cell(row, 1).Value = "FULL YEAR TOTAL (UZS)";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightCyan;
                col = 2;
                foreach (var year in data.Years)
                {
                    var total = data.Totals.FullYearUzsByYear.GetValueOrDefault(year, 0);
                    ws.Cell(row, col).Value = total;
                    ws.Cell(row, col).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(row, col).Style.Font.Bold = true;
                    ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightCyan;
                    col++;
                }
            }
            row += 3;

            // === YEAR-OVER-YEAR COMPARISON ===
            if (data.YearComparisons.Any())
            {
                ws.Cell(row, 1).Value = "YEAR-OVER-YEAR COMPARISON (% Change)";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Range(row, 1, row, data.YearComparisons.Count + 1).Merge();
                ws.Range(row, 1, row, data.YearComparisons.Count + 1).Style.Fill.BackgroundColor = XLColor.LightCoral;
                row++;

                // Headers
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

                // Monthly comparisons - ALL 12 months
                for (int month = 1; month <= 12; month++)
                {
                    var monthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
                    var isSelected = selectedMonths.Contains(month);
                    
                    ws.Cell(row, 1).Value = monthName;
                    if (isSelected)
                    {
                        ws.Cell(row, 1).Style.Font.Bold = true;
                    }
                    else
                    {
                        ws.Cell(row, 1).Style.Font.FontColor = XLColor.Gray;
                    }
                    
                    col = 2;
                    foreach (var comparison in data.YearComparisons)
                    {
                        var percentChange = comparison.MonthlyPercentageChange.GetValueOrDefault(month, 0);
                        ws.Cell(row, col).Value = percentChange / 100;
                        ws.Cell(row, col).Style.NumberFormat.Format = "+0.00%;-0.00%;0%";

                        if (isSelected)
                        {
                            ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightGoldenrodYellow;
                            if (percentChange > 0)
                                ws.Cell(row, col).Style.Font.FontColor = XLColor.Green;
                            else if (percentChange < 0)
                                ws.Cell(row, col).Style.Font.FontColor = XLColor.Red;
                        }
                        else
                        {
                            ws.Cell(row, col).Style.Font.FontColor = XLColor.Gray;
                        }

                        col++;
                    }
                    row++;
                }

                // Selected months total comparison row
                ws.Cell(row, 1).Value = isPartialYear ? "SELECTED MONTHS CHANGE" : "TOTAL CHANGE";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
                col = 2;
                foreach (var comparison in data.YearComparisons)
                {
                    var percentChange = comparison.TotalPercentageChange;
                    ws.Cell(row, col).Value = percentChange / 100;
                    ws.Cell(row, col).Style.NumberFormat.Format = "+0.00%;-0.00%;0%";
                    ws.Cell(row, col).Style.Font.Bold = true;
                    ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightYellow;

                    if (percentChange > 0)
                        ws.Cell(row, col).Style.Font.FontColor = XLColor.Green;
                    else if (percentChange < 0)
                        ws.Cell(row, col).Style.Font.FontColor = XLColor.Red;

                    col++;
                }
                row++;

                // Full year comparison row
                if (isPartialYear)
                {
                    ws.Cell(row, 1).Value = "FULL YEAR CHANGE";
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightCyan;
                    col = 2;
                    foreach (var comparison in data.YearComparisons)
                    {
                        var percentChange = comparison.FullYearPercentageChange;
                        ws.Cell(row, col).Value = percentChange / 100;
                        ws.Cell(row, col).Style.NumberFormat.Format = "+0.00%;-0.00%;0%";
                        ws.Cell(row, col).Style.Font.Bold = true;
                        ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.LightCyan;

                        if (percentChange > 0)
                            ws.Cell(row, col).Style.Font.FontColor = XLColor.Green;
                        else if (percentChange < 0)
                            ws.Cell(row, col).Style.Font.FontColor = XLColor.Red;

                        col++;
                    }
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