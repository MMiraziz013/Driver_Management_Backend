using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Bonus;

public class BonusCalculationExceptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string CompanyNameContains { get; set; } = string.Empty;
    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public BonusCalculationMethod CalculationMethod { get; set; }
    public string CalculationMethodName => CalculationMethod.ToString();

    // Empty = all vehicle types
    public List<string> VehicleTypes { get; set; } = new();

    // Empty = any route
    public List<string> LocationKeywords { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
