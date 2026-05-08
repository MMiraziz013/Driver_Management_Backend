using System.Globalization;
using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Accounting;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;

namespace Clean.Application.Services.Accounting;

public class AccountingUploadService : IAccountingUploadService
{
    private readonly IUnitOfWork _uow;

    public AccountingUploadService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<AccountingUploadResultDto>> UploadReportAsync(
    IFormFile file, int year, int month, string? uploadedBy = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return new Response<AccountingUploadResultDto>(HttpStatusCode.BadRequest,
                    new List<string> { "No file uploaded" });
            }

            // Check if report already exists for this month
            var existingReport = await _uow.AccountingReports.GetByYearAndMonthAsync(year, month);
            if (existingReport != null)
            {
                var existingTransactions = await _uow.AccountingTransactions.GetByReportIdAsync(existingReport.Id);
                _uow.AccountingTransactions.DeleteRange(existingTransactions);
                _uow.AccountingReports.Delete(existingReport);
                await _uow.CompleteAsync();
            }

            var transactions = new List<AccountingTransaction>();
            var warnings = new List<string>();
            int skippedRows = 0;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();

            // Row 7 contains headers (first 6 rows are title/metadata)
            const int headerRowNumber = 7;
            const int dataStartRowNumber = 8;

            var headerRow = worksheet.Row(headerRowNumber);
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in headerRow.CellsUsed())
            {
                var headerName = cell.GetString()?.Trim();
                if (!string.IsNullOrEmpty(headerName))
                {
                    columnMap[headerName] = cell.Address.ColumnNumber;
                }
            }

            // Log found columns for debugging
            if (!columnMap.ContainsKey("Trip Total"))
            {
                warnings.Add("Required column 'Trip Total' not found in header row 7");
            }
            if (!columnMap.ContainsKey("Type"))
            {
                warnings.Add("Required column 'Type' not found in header row 7");
            }

            // Create the report record first
            var report = new AccountingReport
            {
                Year = year,
                Month = month,
                FileName = file.FileName,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = uploadedBy
            };

            await _uow.AccountingReports.AddAsync(report);
            await _uow.CompleteAsync();

            // Process data rows starting from row 8
            var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? dataStartRowNumber;
            
            for (int rowNum = dataStartRowNumber; rowNum <= lastRowUsed; rowNum++)
            {
                var row = worksheet.Row(rowNum);
                
                try
                {
                    // Check if this is the end of data (empty row or "Date Range:" marker)
                    var firstCellValue = row.Cell(1).GetString()?.Trim() ?? "";
                    
                    // Stop if we hit "Date Range:" or similar footer content
                    if (firstCellValue.StartsWith("Date Range", StringComparison.OrdinalIgnoreCase) ||
                        firstCellValue.StartsWith("Total", StringComparison.OrdinalIgnoreCase) ||
                        firstCellValue.StartsWith("Report", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    var typeStr = GetCellValue(row, columnMap, "Type");
                    var tripTotalStr = GetCellValue(row, columnMap, "Trip Total");

                    // Skip empty rows
                    if (string.IsNullOrWhiteSpace(typeStr) && string.IsNullOrWhiteSpace(tripTotalStr))
                    {
                        continue;
                    }

                    // Parse type
                    TransactionType type;
                    if (typeStr.Equals("INH", StringComparison.OrdinalIgnoreCase))
                    {
                        type = TransactionType.INH;
                    }
                    else if (typeStr.Equals("FOT", StringComparison.OrdinalIgnoreCase))
                    {
                        type = TransactionType.FOT;
                    }
                    else
                    {
                        type = TransactionType.INH;
                    }

                    // Parse trip total
                    decimal tripTotal = ParseAmount(tripTotalStr) * 1000;

                    if (tripTotal == 0 && !string.IsNullOrWhiteSpace(tripTotalStr))
                    {
                        warnings.Add($"Row {rowNum}: Could not parse amount '{tripTotalStr}'");
                    }

                    var transaction = new AccountingTransaction
                    {
                        AccountingReportId = report.Id,
                        Year = year,
                        Month = month,
                        Type = type,
                        AffiliateFirstName = GetCellValue(row, columnMap, "Affiliate First Name"),
                        AffiliateLastName = GetCellValue(row, columnMap, "Affiliate Last Name"),
                        BillingContact = GetCellValue(row, columnMap, "Billing Contact"),
                        BookingContact = GetCellValue(row, columnMap, "Booking Contact"),
                        PassengerFirstName = GetCellValue(row, columnMap, "Passenger First Name"),
                        Company = GetCellValue(row, columnMap, "Company"),
                        Car = GetCellValue(row, columnMap, "Car"),
                        VehicleType = GetCellValue(row, columnMap, "Vehicle Type"),
                        ServiceType = GetCellValue(row, columnMap, "Service Type"),
                        Status = GetCellValue(row, columnMap, "Status"),
                        PmtMethod = GetCellValue(row, columnMap, "Pmt Method"),
                        TripTotal = tripTotal
                    };

                    transactions.Add(transaction);
                }
                catch (Exception ex)
                {
                    warnings.Add($"Row {rowNum}: Error - {ex.Message}");
                    skippedRows++;
                }
            }

            // Save transactions
            if (transactions.Any())
            {
                await _uow.AccountingTransactions.AddRangeAsync(transactions);
            }

            // Update report totals
            report.TransactionCount = transactions.Count;
            report.TotalAmount = transactions.Sum(t => t.TripTotal);
            _uow.AccountingReports.Update(report);

            await _uow.CompleteAsync();

            var result = new AccountingUploadResultDto
            {
                Year = year,
                Month = month,
                TransactionsImported = transactions.Count,
                TransactionsSkipped = skippedRows,
                TotalAmount = report.TotalAmount,
                Warnings = warnings.Take(20).ToList()
            };

            return new Response<AccountingUploadResultDto>(HttpStatusCode.OK,
                $"Imported {transactions.Count} transactions for {GetMonthName(month)} {year}", result);
        }
        catch (Exception ex)
        {
            return new Response<AccountingUploadResultDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message, ex.StackTrace ?? "" });
        }
    }

    /// <summary>
    /// Get cell value by column name from the column map
    /// </summary>
    private static string GetCellValue(IXLRow row, Dictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out var colNumber))
        {
            return string.Empty;
        }
        
        var cell = row.Cell(colNumber);
        return cell.GetString()?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Parse amount from various formats: "840.00", "1,234.56", "1 234.56", etc.
    /// </summary>
    private static decimal ParseAmount(string amountStr)
    {
        if (string.IsNullOrWhiteSpace(amountStr))
            return 0;

        // Clean up the string
        var clean = amountStr
            .Replace("$", "")
            .Replace("₽", "")
            .Replace("сум", "")
            .Replace("UZS", "")
            .Replace(" ", "")  // Remove spaces (thousand separator in some locales)
            .Trim();

        // Handle comma as thousand separator (1,234.56 -> 1234.56)
        // If there's both comma and dot, comma is likely thousand separator
        if (clean.Contains(',') && clean.Contains('.'))
        {
            clean = clean.Replace(",", "");
        }
        // If only comma and it's followed by exactly 2 digits at end, it's decimal separator
        else if (clean.Contains(','))
        {
            var commaIndex = clean.LastIndexOf(',');
            var afterComma = clean.Substring(commaIndex + 1);
            
            if (afterComma.Length == 2 && afterComma.All(char.IsDigit))
            {
                // Comma is decimal separator (European format)
                clean = clean.Replace(",", ".");
            }
            else
            {
                // Comma is thousand separator
                clean = clean.Replace(",", "");
            }
        }

        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return 0;
    }

    private static string GetMonthName(int month)
    {
        return CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
    }

    private static AccountingReportDto MapToDto(AccountingReport report) => new()
    {
        Id = report.Id,
        Year = report.Year,
        Month = report.Month,
        MonthName = GetMonthName(report.Month),
        FileName = report.FileName,
        TransactionCount = report.TransactionCount,
        TotalAmount = report.TotalAmount,
        UploadedAt = report.UploadedAt,
        UploadedBy = report.UploadedBy
    };

    public async Task<Response<List<AccountingReportDto>>> GetAllReportsAsync()
    {
        try
        {
            var reports = await _uow.AccountingReports.GetAllAsync();
            var dtos = reports.Select(MapToDto).ToList();
            return new Response<List<AccountingReportDto>>(HttpStatusCode.OK, dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<AccountingReportDto>>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<List<AccountingReportDto>>> GetReportsByYearAsync(int year)
    {
        try
        {
            var reports = await _uow.AccountingReports.GetByYearAsync(year);
            var dtos = reports.Select(MapToDto).ToList();
            return new Response<List<AccountingReportDto>>(HttpStatusCode.OK, dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<AccountingReportDto>>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<string>> DeleteReportAsync(int year, int month)
    {
        try
        {
            var report = await _uow.AccountingReports.GetByYearAndMonthAsync(year, month);
            if (report == null)
            {
                return new Response<string>(HttpStatusCode.NotFound,
                    new List<string> { $"Report for {GetMonthName(month)} {year} not found" });
            }

            _uow.AccountingReports.Delete(report);
            await _uow.CompleteAsync();

            return new Response<string>(HttpStatusCode.OK,
                $"Deleted report for {GetMonthName(month)} {year} ({report.TransactionCount} transactions)");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }
}