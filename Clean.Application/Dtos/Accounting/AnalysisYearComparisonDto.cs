namespace Clean.Application.Dtos.Accounting;

public class AnalysisYearComparisonDto
{
    /// <summary>
    /// Base year for comparison
    /// </summary>
    public int BaseYear { get; set; }
    
    /// <summary>
    /// Comparison year
    /// </summary>
    public int CompareYear { get; set; }
    
    /// <summary>
    /// Monthly percentage changes: { 1: 15.5, 2: -10.2, ... }
    /// </summary>
    public Dictionary<int, decimal> MonthlyPercentageChange { get; set; } = new();
    
    /// <summary>
    /// Overall percentage change
    /// </summary>
    public decimal TotalPercentageChange { get; set; }
}