namespace Clean.Application.Dtos.Accounting;

public class AnalysisMonthRowDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public Dictionary<int, decimal> AmountsByYear { get; set; } = new();
    public bool IsSelected { get; set; } = true;
}