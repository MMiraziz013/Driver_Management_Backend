namespace Clean.Application.Dtos.Accounting;

public class AnalysisYearComparisonDto
{
    public int BaseYear { get; set; }
    public int CompareYear { get; set; }
    
    // Monthly percentage changes (selected months only)
    public Dictionary<int, decimal> MonthlyPercentageChange { get; set; } = new();
    
    // Selected months total percentage change
    public decimal TotalPercentageChange { get; set; }
    
    // Full year percentage change
    public decimal FullYearPercentageChange { get; set; }
}