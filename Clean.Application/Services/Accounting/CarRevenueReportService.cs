using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Accounting;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Clean.Application.Services.Accounting;

public class CarRevenueReportService : ICarRevenueReportService
{
    private readonly IUnitOfWork _uow;

    public CarRevenueReportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<CarRevenueReportDto>> GenerateReportAsync(CarRevenueReportRequestDto request)
        {
            try
            {
                if (request.Year <= 0)
                {
                    return new Response<CarRevenueReportDto>(HttpStatusCode.BadRequest,
                        new List<string> { "Year is required" });
                }

                // If no months specified, include all 12 months
                var selectedMonths = (request.Months != null && request.Months.Any())
                    ? request.Months.OrderBy(m => m).ToList()
                    : Enumerable.Range(1, 12).ToList();

                // Get exchange rate for the year
                var exchangeRateEntity = await _uow.ExchangeRates.GetByYearAsync(request.Year);
                var exchangeRate = exchangeRateEntity?.Rate ?? 12500m;

                // Get all transactions for the year and selected months - ONLY INH TYPE
                var transactions = await _uow.AccountingTransactions.GetByYearAsync(request.Year);
                var filteredTransactions = transactions
                    .Where(t => selectedMonths.Contains(t.Month))
                    .Where(t => t.Type == TransactionType.INH)  // Only In-House transactions
                    .ToList();

                // Get all vehicles for matching and getting purchase cost
                var vehicles = await _uow.Vehicles.GetActiveAndInactiveAsync();
                
                // Build multiple lookup keys for each vehicle (for flexible matching)
                var vehicleLookup = new Dictionary<string, Domain.Entities.Vehicle>(StringComparer.OrdinalIgnoreCase);
                foreach (var vehicle in vehicles)
                {
                    var plate = vehicle.PlateNumber?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(plate))
                    {
                        // Add the exact plate
                        vehicleLookup.TryAdd(plate, vehicle);
                        vehicleLookup.TryAdd(plate.ToUpperInvariant(), vehicle);
                        
                        // Also add without spaces
                        vehicleLookup.TryAdd(plate.Replace(" ", ""), vehicle);
                    }
                }

                // Group transactions by car (plate number)
                var groupedByCar = filteredTransactions
                    .Where(t => !string.IsNullOrWhiteSpace(t.Car))
                    .GroupBy(t => t.Car!.Trim())
                    .ToList();

                var grandTotal = filteredTransactions.Sum(t => t.TripTotal);
                var rows = new List<CarRevenueRowDto>();

                foreach (var carGroup in groupedByCar)
                {
                    var carPlate = carGroup.Key;
                    if (carPlate == "N/A") continue;
                    var carTransactions = carGroup.ToList();

                    // Try to find the vehicle in the system using flexible matching
                    var vehicle = FindVehicle(carPlate, vehicleLookup);

                    // Get category from transactions (or from vehicle if found)
                    var category = carTransactions
                        .Select(t => t.VehicleType)
                        .FirstOrDefault(vt => !string.IsNullOrWhiteSpace(vt)) ?? "Unknown";

                    // Calculate totals
                    var totalAmount = carTransactions.Sum(t => t.TripTotal);
                    var monthCount = selectedMonths.Count;
                    var averageUzs = monthCount > 0 ? totalAmount / monthCount : 0;
                    var averageUsd = exchangeRate > 0 ? averageUzs / exchangeRate : 0;

                    // Get car cost and plan from vehicle entity
                    var carCostUsd = vehicle?.PurchaseCostUsd ?? 0;
                    var planMonths = vehicle?.PlanMonths ?? 13;
                    var planUsd = planMonths > 0 ? carCostUsd / planMonths : 0;

                    // Calculate monthly amounts and trips
                    var monthlyAmounts = new Dictionary<int, decimal>();
                    var monthlyTripCounts = new Dictionary<int, int>();

                    foreach (var month in selectedMonths)
                    {
                        var monthTransactions = carTransactions.Where(t => t.Month == month).ToList();
                        monthlyAmounts[month] = monthTransactions.Sum(t => t.TripTotal);
                        monthlyTripCounts[month] = monthTransactions.Count;
                    }

                    // Calculate portion
                    var portionPercent = grandTotal > 0 
                        ? Math.Round((totalAmount / grandTotal) * 100, 2) 
                        : 0;

                    rows.Add(new CarRevenueRowDto
                    {
                        Car = carPlate,
                        Category = category,
                        TotalAmount = totalAmount,
                        AverageUzs = Math.Round(averageUzs, 2),
                        AverageUsd = Math.Round(averageUsd, 2),
                        CarCostUsd = carCostUsd,
                        PlanUsd = Math.Round(planUsd, 2),
                        PlanMonths = planMonths,
                        MonthlyAmounts = monthlyAmounts,
                        MonthlyTripCounts = monthlyTripCounts,
                        PortionPercent = portionPercent,
                        TripCount = carTransactions.Count
                    });
                }

                // Sort by total amount descending
                rows = rows.OrderByDescending(r => r.TotalAmount).ToList();

                // Calculate totals
                var totals = new CarRevenueTotalsDto
                {
                    TotalAmount = rows.Sum(r => r.TotalAmount),
                    AverageUzs = rows.Sum(r => r.AverageUzs),
                    AverageUsd = rows.Sum(r => r.AverageUsd),
                    TotalCarCostUsd = rows.Sum(r => r.CarCostUsd),
                    TotalPlanUsd = rows.Sum(r => r.PlanUsd),
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

                var result = new CarRevenueReportDto
                {
                    Year = request.Year,
                    Months = selectedMonths,
                    MonthNames = monthNames,
                    ExchangeRate = exchangeRate,
                    Rows = rows,
                    Totals = totals,
                    GeneratedAt = DateTime.UtcNow
                };

                return new Response<CarRevenueReportDto>(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return new Response<CarRevenueReportDto>(HttpStatusCode.InternalServerError,
                    new List<string> { ex.Message, ex.StackTrace ?? "" });
            }
        }

    /// <summary>
    /// Find a vehicle by matching the car string from the report.
    /// Handles formats like "Sprinter (01/401OLA)" or "Toyota Hiace(01/174FHA)"
    /// </summary>
    private Domain.Entities.Vehicle? FindVehicle(
        string carString, 
        Dictionary<string, Domain.Entities.Vehicle> vehicleLookup)
    {
        if (string.IsNullOrWhiteSpace(carString))
            return null;

        // Try exact match first
        if (vehicleLookup.TryGetValue(carString, out var exactMatch))
            return exactMatch;

        // Extract plate number from parentheses: "Sprinter (01/401OLA)" -> "01/401OLA"
        var plateMatch = Regex.Match(carString, @"\(([^)]+)\)");
        if (plateMatch.Success)
        {
            var extractedPlate = plateMatch.Groups[1].Value.Trim();
            
            if (vehicleLookup.TryGetValue(extractedPlate, out var matchByExtracted))
                return matchByExtracted;
                
            // Try without spaces
            if (vehicleLookup.TryGetValue(extractedPlate.Replace(" ", ""), out var matchNoSpaces))
                return matchNoSpaces;
        }

        // Try matching just the plate number pattern (e.g., "01/401OLA")
        var platePattern = Regex.Match(carString, @"\d{2}/\d{2,3}[A-Z]{2,3}");
        if (platePattern.Success)
        {
            var plateNum = platePattern.Value;
            if (vehicleLookup.TryGetValue(plateNum, out var matchByPattern))
                return matchByPattern;
        }

        // Try partial matching - check if any vehicle plate is contained in the car string
        foreach (var kvp in vehicleLookup)
        {
            if (!string.IsNullOrEmpty(kvp.Key) && 
                carString.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    public async Task<Response<byte[]>> ExportToExcelAsync(CarRevenueReportRequestDto request)
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
            CreateCarRevenueSheet(workbook, "Total", data);

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

    private void CreateCarRevenueSheet(XLWorkbook workbook, string sheetName, CarRevenueReportDto data)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        int row = 1;
        int col;

        // Title
        ws.Cell(row, 1).Value = $"CAR REVENUE REPORT - {data.Year}";
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
            "Car",
            "Category",
            $"Total ({data.Months.Count} months)",
            "Aver. UZS",
            "Aver. USD",
            "Car Cost (USD)",
            "Plan (USD)"
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
        foreach (var carRow in data.Rows)
        {
            col = 1;
            ws.Cell(row, col++).Value = carRow.Car;
            ws.Cell(row, col++).Value = carRow.Category;
            
            ws.Cell(row, col).Value = carRow.TotalAmount;
            ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";
            
            ws.Cell(row, col).Value = carRow.AverageUzs;
            ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";
            
            ws.Cell(row, col).Value = carRow.AverageUsd;
            ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0.00";
            
            ws.Cell(row, col).Value = carRow.CarCostUsd;
            ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0.00";
            
            ws.Cell(row, col).Value = carRow.PlanUsd;
            ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0.00";

            // Monthly amounts
            foreach (var month in data.Months)
            {
                var amount = carRow.MonthlyAmounts.GetValueOrDefault(month, 0);
                ws.Cell(row, col).Value = amount;
                ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";
            }

            // Portion
            ws.Cell(row, col).Value = carRow.PortionPercent / 100;
            ws.Cell(row, col).Style.NumberFormat.Format = "0.00%";

            row++;
        }

        // Totals row
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Range(row, 1, row, headers.Count).Style.Fill.BackgroundColor = XLColor.LightYellow;
        ws.Range(row, 1, row, headers.Count).Style.Font.Bold = true;

        col = 3;
        ws.Cell(row, col).Value = data.Totals.TotalAmount;
        ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";

        ws.Cell(row, col).Value = data.Totals.AverageUzs;
        ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0";

        ws.Cell(row, col).Value = data.Totals.AverageUsd;
        ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0.00";

        ws.Cell(row, col).Value = data.Totals.TotalCarCostUsd;
        ws.Cell(row, col++).Style.NumberFormat.Format = "#,##0.00";

        ws.Cell(row, col).Value = data.Totals.TotalPlanUsd;
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

    private void CreateMonthSheet(XLWorkbook workbook, string sheetName, CarRevenueReportDto data, int month)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        int row = 1;

        // Title
        ws.Cell(row, 1).Value = $"{sheetName} {data.Year} - Car Revenue";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        row += 2;

        // Headers
        var headers = new[] { "Car", "Car Type", "Trips", "Amount (UZS)", "Portion %" };
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
                r.Car,
                r.Category,
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

            ws.Cell(row, 1).Value = dataRow.Car;
            ws.Cell(row, 2).Value = dataRow.Category;
            ws.Cell(row, 3).Value = dataRow.Trips;
            ws.Cell(row, 4).Value = dataRow.Amount;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 5).Value = monthPortion / 100;
            ws.Cell(row, 5).Style.NumberFormat.Format = "0.00%";
            row++;
        }

        // Totals row
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Range(row, 1, row, 2).Merge();
        ws.Cell(row, 3).Value = monthTotalTrips;
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = monthTotal;
        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = 1;
        ws.Cell(row, 5).Style.NumberFormat.Format = "0.00%";
        ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.LightYellow;

        ws.Columns().AdjustToContents();
    }    
}