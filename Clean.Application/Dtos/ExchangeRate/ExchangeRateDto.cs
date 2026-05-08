namespace Clean.Application.Dtos.ExchangeRate;

public class ExchangeRateDto
{
    public int Id { get; set; }
    public int Year { get; set; }
    public decimal Rate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}