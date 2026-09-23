using Clean.Domain.Enums;

namespace Clean.Application.Dtos.Bonus;

/// <summary>
/// Used for both create and update of a bonus calculation exception.
/// </summary>
public class SaveBonusCalculationExceptionDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string CompanyNameContains { get; set; } = string.Empty;
    public int ServiceTypeId { get; set; }
    public BonusCalculationMethod CalculationMethod { get; set; } = BonusCalculationMethod.DurationBased;

    // Vehicle type names (as in /api/vehicle-types). Empty = all vehicle types
    public List<string>? VehicleTypes { get; set; }

    // Location keywords matched against route points (PU/ST/DO), e.g. ["Chirchik", "Chirchiq"]. Empty = any route
    public List<string>? LocationKeywords { get; set; }
}
