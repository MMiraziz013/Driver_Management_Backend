using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Bonus;

public class ServiceTypeBonusConfigDto
{
    public int Id { get; set; }
    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public BonusCalculationMethod CalculationMethod { get; set; }
    public string CalculationMethodName => CalculationMethod.ToString();
}