namespace Clean.Application.Dtos.Accounting;

public class AccountingReportDto
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
}