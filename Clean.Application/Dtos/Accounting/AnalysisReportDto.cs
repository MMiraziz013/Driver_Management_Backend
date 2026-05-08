namespace Clean.Application.Dtos.Accounting;

public class AnalysisReportDto
{
    public List<int> Years { get; set; } = new();
    public List<int> Months { get; set; } = new();
    public List<AnalysisMonthRowDto> MonthlyData { get; set; } = new();
    public AnalysisTotalsDto Totals { get; set; } = new();
    public List<AnalysisYearComparisonDto> YearComparisons { get; set; } = new();
    public Dictionary<int, decimal> ExchangeRates { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}