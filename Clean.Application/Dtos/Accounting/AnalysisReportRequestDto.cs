namespace Clean.Application.Dtos.Accounting;

public class AnalysisReportRequestDto
{
    /// <summary>
    /// Years to include in the report (e.g., [2021, 2022, 2023, 2024, 2025, 2026])
    /// </summary>
    public List<int> Years { get; set; } = new();
    
    /// <summary>
    /// Months to include (1-12). If empty, all months are included.
    /// </summary>
    public List<int> Months { get; set; } = new();
}