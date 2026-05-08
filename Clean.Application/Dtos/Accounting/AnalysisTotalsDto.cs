namespace Clean.Application.Dtos.Accounting;

public class AnalysisTotalsDto
{
    /// <summary>
    /// Total in USD per year: { 2021: 12000, 2022: 18000, ... }
    /// </summary>
    public Dictionary<int, decimal> TotalUsdByYear { get; set; } = new();
    
    /// <summary>
    /// Total in UZS per year (using exchange rate): { 2021: 120000000, ... }
    /// </summary>
    public Dictionary<int, decimal> TotalUzsByYear { get; set; } = new();
}