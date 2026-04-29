using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Bonus;

public class UpdateServiceTypeBonusConfigDto
{
    public int ServiceTypeId { get; set; }
    public BonusCalculationMethod CalculationMethod { get; set; }
}