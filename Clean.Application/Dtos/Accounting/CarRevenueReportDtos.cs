namespace Clean.Application.Dtos.Accounting;

public class CarRevenueReportRequestDto
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

public class CarRevenueReportDto
{
    public int Year { get; set; }
    public List<int> Months { get; set; } = new();
    public List<string> MonthNames { get; set; } = new();
    public decimal ExchangeRate { get; set; }
    public List<CarRevenueRowDto> Rows { get; set; } = new();
    public CarRevenueTotalsDto Totals { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class CarRevenueRowDto
{
    /// <summary>
    /// Plate number of the car
    /// </summary>
    public string Car { get; set; } = string.Empty;
    
    /// <summary>
    /// Vehicle type/category
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Total amount for all selected months (in UZS)
    /// </summary>
    public decimal TotalAmount { get; set; }
    
    /// <summary>
    /// Average per month in UZS
    /// </summary>
    public decimal AverageUzs { get; set; }
    
    /// <summary>
    /// Average per month in USD
    /// </summary>
    public decimal AverageUsd { get; set; }
    
    /// <summary>
    /// Car purchase cost in USD
    /// </summary>
    public decimal CarCostUsd { get; set; }
    
    /// <summary>
    /// Monthly plan (CarCost / PlanMonths) in USD
    /// </summary>
    public decimal PlanUsd { get; set; }
    
    /// <summary>
    /// Number of months used for plan calculation
    /// </summary>
    public int PlanMonths { get; set; }
    
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

    public decimal CostUsd { get; set; }
    
    /// <summary>
    /// Trip count per month: { 1: 25, 2: 30, ... }
    /// </summary>
    public Dictionary<int, int> MonthlyTripCounts { get; set; } = new();

}

public class CarRevenueTotalsDto
{
    public decimal TotalAmount { get; set; }
    public decimal AverageUzs { get; set; }
    public decimal AverageUsd { get; set; }
    public decimal TotalCarCostUsd { get; set; }
    public decimal TotalPlanUsd { get; set; }
    public Dictionary<int, decimal> MonthlyAmounts { get; set; } = new();
    public int TotalTripCount { get; set; }
}