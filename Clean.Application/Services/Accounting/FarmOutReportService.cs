using System.Globalization;
using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Accounting;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Enums;
using ClosedXML.Excel;

namespace Clean.Application.Services.Accounting;

public class FarmOutReportService : IFarmOutReportService
{
    private readonly IUnitOfWork _uow;

    public FarmOutReportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<FarmOutReportDto>> GenerateReportAsync(FarmOutReportRequestDto request)
    {
        try
        {
            if (request.Year <= 0)
            {
                return new Response<FarmOutReportDto>(HttpStatusCode.BadRequest,
                    new List<string> { "Year is required" });
            }

            // If no months specified, include all 12 months
            var selectedMonths = (request.Months != null && request.Months.Any())
                ? request.Months.OrderBy(m => m).ToList()
                : Enumerable.Range(1, 12).ToList();

            // Get exchange rate for the year
            var exchangeRateEntity = await _uow.ExchangeRates.GetByYearAsync(request.Year);
            var exchangeRate = exchangeRateEntity?.Rate ?? 12500m;

            // Get all transactions for the year and selected months - ONLY FOT TYPE
            var transactions = await _uow.AccountingTransactions.GetByYearAsync(request.Year);
            var filteredTransactions = transactions
                .Where(t => selectedMonths.Contains(t.Month))
                .Where(t => t.Type == TransactionType.FOT)  // Only Farm Out transactions
                .ToList();

            // Get all vehicles to find purchase cost by vehicle type
            var vehicles = await _uow.Vehicles.GetActiveAndInactiveAsync();
            
            // Build lookup for vehicle type to purchase cost
            // Get the first non-zero purchase cost for each vehicle type name
            var vehicleTypes = await _uow.VehicleTypes.GetAllAsync();
            var vehicleTypeCosts = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var vt in vehicleTypes)
            {
                // Find any vehicle of this type with a purchase cost
                var vehicleWithCost = vehicles
                    .FirstOrDefault(v => v.VehicleTypeId == vt.Id && v.PurchaseCostUsd > 0);
                
                if (vehicleWithCost != null)
                {
                    vehicleTypeCosts[vt.Name] = vehicleWithCost.PurchaseCostUsd;
                }
            }

            // Group transactions by Vehicle Type
            var groupedByType = filteredTransactions
                .Where(t => !string.IsNullOrWhiteSpace(t.VehicleType))
                .GroupBy(t => t.VehicleType!.Trim())
                .ToList();

            var grandTotal = filteredTransactions.Sum(t => t.TripTotal);
            var rows = new List<FarmOutRowDto>();

            foreach (var typeGroup in groupedByType)
            {
                var carType = typeGroup.Key;
                var typeTransactions = typeGroup.ToList();

                if (carType == "Business Hall")
                {
                    continue;
                }

                // Calculate totals
                var total = typeTransactions.Sum(t => t.TripTotal);
                var totalUsd = exchangeRate > 0 ? total / exchangeRate : 0;

                // Get car cost for this vehicle type
                var carCostUsd = vehicleTypeCosts.GetValueOrDefault(carType, 0);

                // Calculate monthly amounts and trips
                var monthlyAmounts = new Dictionary<int, decimal>();
                var monthlyTripCounts = new Dictionary<int, int>();

                foreach (var month in selectedMonths)
                {
                    var monthTransactions = typeTransactions.Where(t => t.Month == month).ToList();
                    monthlyAmounts[month] = monthTransactions.Sum(t => t.TripTotal);
                    monthlyTripCounts[month] = monthTransactions.Count;
                }

                // Calculate portion
                var portionPercent = grandTotal > 0 
                    ? Math.Round((total / grandTotal) * 100, 2) 
                    : 0;

                rows.Add(new FarmOutRowDto
                {
                    CarType = carType,
                    Total = total,
                    TotalUsd = Math.Round(totalUsd, 2),
                    CarCostUsd = carCostUsd,
                    MonthlyAmounts = monthlyAmounts,
                    MonthlyTripCounts = monthlyTripCounts,
                    PortionPercent = portionPercent,
                    TripCount = typeTransactions.Count
                });
            }

            // Sort by total amount descending
            rows = rows.OrderByDescending(r => r.Total).ToList();

            // Calculate totals
            var totals = new FarmOutTotalsDto
            {
                Total = rows.Sum(r => r.Total),
                TotalUsd = rows.Sum(r => r.TotalUsd),
                TotalCarCostUsd = rows.Sum(r => r.CarCostUsd),
                TotalTripCount = rows.Sum(r => r.TripCount),
                MonthlyAmounts = new Dictionary<int, decimal>()
            };

            foreach (var month in selectedMonths)
            {
                totals.MonthlyAmounts[month] = rows.Sum(r => r.MonthlyAmounts.GetValueOrDefault(month, 0));
            }

            // Build month names
            var monthNames = selectedMonths
                .Select(m => CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(m))
                .ToList();

            var result = new FarmOutReportDto
            {
                Year = request.Year,
                Months = selectedMonths,
                MonthNames = monthNames,
                ExchangeRate = exchangeRate,
                Rows = rows,
                Totals = totals,
                GeneratedAt = DateTime.UtcNow
            };

            return new Response<FarmOutReportDto>(HttpStatusCode.OK, result);
        }
        catch (Exception ex)
        {
            return new Response<FarmOutReportDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message, ex.StackTrace ?? "" });
        }
    }

    public async Task<Response<byte[]>> ExportToExcelAsync(FarmOutReportRequestDto request)
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
            
            // Create Total sheet
            CreateFarmOutSheet(workbook, "Total", data);

            // Create individual month sheets
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

    private void CreateFarmOutSheet(XLWorkbook workbook, string sheetName, FarmOutReportDto data)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        int row = 1;
        int col;

        // Title
        ws.Cell(row, 1).Value = $"FARM OUT REPORT - {data.Year}";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 16;
        row++;

        ws.Cell(row, 1).Value = $"Months: {string.Join(", ", data.MonthNames)}";
        row++;

        ws.Cell(row, 1).Value = $"Exchange Rate: {data.ExchangeRate:N2} UZS/USD";
        row += 2;

        // Headers
        var headers = new List<string>
        {
            "Car Type",
            "Total (UZS)",
            "Total (USD)",
            "Car Cost (USD)"
        };

        // Add month headers
        foreach (var month in data.Months)
        {
            headers.Add(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month));
        }

        headers.Add("Portion %");

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
        foreach (var dataRow in data.Rows)
        {
            col = 1;
            ws.Cell(row, col++).Value = dataRow.CarType;
            
            ws.Cell(row, col).Value = dataRow.Total;
            ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";
            
            ws.Cell(row, col).Value = dataRow.TotalUsd;
            ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0.00";
            
            ws.Cell(row, col).Value = dataRow.CarCostUsd;
            ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0.00";

            // Monthly amounts
            foreach (var month in data.Months)
            {
                var amount = dataRow.MonthlyAmounts.GetValueOrDefault(month, 0);
                ws.Cell(row, col).Value = amount;
                ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";
            }

            // Portion
            ws.Cell(row, col).Value = dataRow.PortionPercent / 100;
            ws.Cell(row, col).Style.NumberFormat.Format = "0.00%";

            row++;
        }

        // Totals row
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Range(row, 1, row, headers.Count).Style.Fill.BackgroundColor = XLColor.LightYellow;
        ws.Range(row, 1, row, headers.Count).Style.Font.Bold = true;

        col = 2;
        ws.Cell(row, col).Value = data.Totals.Total;
        ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";

        ws.Cell(row, col).Value = data.Totals.TotalUsd;
        ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0.00";

        ws.Cell(row, col).Value = data.Totals.TotalCarCostUsd;
        ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0.00";

        foreach (var month in data.Months)
        {
            var amount = data.Totals.MonthlyAmounts.GetValueOrDefault(month, 0);
            ws.Cell(row, col).Value = amount;
            ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";
        }

        ws.Cell(row, col).Value = 1; // 100%
        ws.Cell(row, col).Style.NumberFormat.Format = "0.00%";

        ws.Columns().AdjustToContents();
    }

    private void CreateMonthSheet(XLWorkbook workbook, string sheetName, FarmOutReportDto data, int month)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        int row = 1;

        // Title
        ws.Cell(row, 1).Value = $"{sheetName} {data.Year} - Farm Out";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        row += 2;

        // Headers
        var headers = new[] { "Car Type", "Quantity", "Amount (UZS)", "Portion %" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
            ws.Cell(row, i + 1).Style.Font.Bold = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            ws.Cell(row, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        row++;

        // Filter rows that have data for this month
        var monthRows = data.Rows
            .Where(r => r.MonthlyAmounts.GetValueOrDefault(month, 0) > 0)
            .Select(r => new
            {
                r.CarType,
                Trips = r.MonthlyTripCounts.GetValueOrDefault(month, 0),
                Amount = r.MonthlyAmounts.GetValueOrDefault(month, 0)
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        var monthTotal = monthRows.Sum(r => r.Amount);
        var monthTotalTrips = monthRows.Sum(r => r.Trips);

        foreach (var dataRow in monthRows)
        {
            var monthPortion = monthTotal > 0 ? (dataRow.Amount / monthTotal) * 100 : 0;

            ws.Cell(row, 1).Value = dataRow.CarType;
            ws.Cell(row, 2).Value = dataRow.Trips;
            ws.Cell(row, 3).Value = dataRow.Amount;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 4).Value = monthPortion / 100;
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.00%";
            row++;
        }

        // Totals row
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = monthTotalTrips;
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = monthTotal;
        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = 1;
        ws.Cell(row, 4).Style.NumberFormat.Format = "0.00%";
        ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightYellow;

        ws.Columns().AdjustToContents();
    }
}