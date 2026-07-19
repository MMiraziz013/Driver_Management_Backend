namespace Clean.Application.Dtos.Accounting;

public class AnalysisTotalsDto
{
    // Selected months totals
    public Dictionary<int, decimal> TotalUsdByYear { get; set; } = new();
    public Dictionary<int, decimal> TotalUzsByYear { get; set; } = new();
    
    // Full year totals (all 12 months)
    public Dictionary<int, decimal> FullYearUsdByYear { get; set; } = new();
    public Dictionary<int, decimal> FullYearUzsByYear { get; set; } = new();
}