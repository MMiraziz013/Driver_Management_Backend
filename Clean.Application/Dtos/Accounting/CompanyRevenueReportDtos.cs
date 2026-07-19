namespace Clean.Application.Dtos.Accounting;

public class CompanyRevenueReportRequestDto
{
    public int Year { get; set; }
    public List<int> Months { get; set; } = new();
}

public class CompanyRevenueReportDto
{
    public int Year { get; set; }
    public List<int> Months { get; set; } = new();
    public List<string> MonthNames { get; set; } = new();
    public decimal ExchangeRate { get; set; }
    
    /// <summary>
    /// Analysis by category
    /// </summary>
    public List<CategoryRevenueRowDto> CategoryAnalysis { get; set; } = new();
    
    /// <summary>
    /// Detailed company data
    /// </summary>
    public List<CompanyRevenueRowDto> CompanyRows { get; set; } = new();
    
    public CompanyRevenueTotalsDto Totals { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class CategoryRevenueRowDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal PortionPercent { get; set; }
    public int CompanyCount { get; set; }
}

public class CompanyRevenueRowDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal PortionPercent { get; set; }
    public Dictionary<int, decimal> MonthlyAmounts { get; set; } = new();
    public int TripCount { get; set; }
}

public class CompanyRevenueTotalsDto
{
    public decimal Total { get; set; }
    public Dictionary<int, decimal> MonthlyAmounts { get; set; } = new();
    public int TotalTripCount { get; set; }
}