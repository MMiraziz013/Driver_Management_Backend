namespace Clean.Application.Dtos.Accounting;

public class FarmOutReportRequestDto
{
    /// <summary>
    /// Year for the report
    /// </summary>
    public int Year { get; set; }
    
    /// <summary>
    /// Months to include (1-12). If empty, all months are included.
    /// </summary>
    public List<int> Months { get; set; } = new();
}

public class FarmOutReportDto
{
    public int Year { get; set; }
    public List<int> Months { get; set; } = new();
    public List<string> MonthNames { get; set; } = new();
    public decimal ExchangeRate { get; set; }
    public List<FarmOutRowDto> Rows { get; set; } = new();
    public FarmOutTotalsDto Totals { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class FarmOutRowDto
{
    /// <summary>
    /// Vehicle type/category (e.g., Sedan, SUV, Minibus)
    /// </summary>
    public string CarType { get; set; } = string.Empty;
    
    /// <summary>
    /// Total amount for all selected months (in UZS)
    /// </summary>
    public decimal Total { get; set; }
    
    /// <summary>
    /// Total amount in USD
    /// </summary>
    public decimal TotalUsd { get; set; }
    
    /// <summary>
    /// Car cost in USD (from any vehicle of this type)
    /// </summary>
    public decimal CarCostUsd { get; set; }
    
    /// <summary>
    /// Amount per month: { 1: 5000000, 2: 6000000, ... }
    /// </summary>
    public Dictionary<int, decimal> MonthlyAmounts { get; set; } = new();
    
    /// <summary>
    /// Portion of total revenue (percentage)
    /// </summary>
    public decimal PortionPercent { get; set; }
    
    /// <summary>
    /// Number of trips
    /// </summary>
    public int TripCount { get; set; }
    
    /// <summary>
    /// Trip count per month: { 1: 25, 2: 30, ... }
    /// </summary>
    public Dictionary<int, int> MonthlyTripCounts { get; set; } = new();

}

public class FarmOutTotalsDto
{
    public decimal Total { get; set; }
    public decimal TotalUsd { get; set; }
    public decimal TotalCarCostUsd { get; set; }
    public Dictionary<int, decimal> MonthlyAmounts { get; set; } = new();
    public int TotalTripCount { get; set; }
}