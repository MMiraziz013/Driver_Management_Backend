namespace Clean.Application.Dtos.Accounting;

public class AnalysisMonthRowDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    
    /// <summary>
    /// Amount per year in UZS: { 2021: 1000000, 2022: 1500000, ... }
    /// </summary>
    public Dictionary<int, decimal> AmountsByYear { get; set; } = new();
}