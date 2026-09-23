using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Clean.Domain.Enums;

namespace Clean.Domain.Entities;

/// <summary>
/// Overrides the bonus calculation method for trips of a given company + service type,
/// optionally narrowed down by vehicle types and route locations
/// (e.g. Sierra Nevada Round Trips via Chirchik are calculated hourly).
/// </summary>
public class BonusCalculationException
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Case-insensitive "contains" match against Trip.CompanyName
    [MaxLength(200)]
    public string CompanyNameContains { get; set; } = string.Empty;

    public int ServiceTypeId { get; set; }

    [ForeignKey(nameof(ServiceTypeId))]
    public ServiceType ServiceType { get; set; } = null!;

    public BonusCalculationMethod CalculationMethod { get; set; } = BonusCalculationMethod.DurationBased;

    // === Vehicle Types (JSON array). Empty = all vehicle types ===
    public string VehicleTypesJson { get; set; } = "[]";

    [NotMapped]
    public List<string> VehicleTypes
    {
        get => DeserializeList(VehicleTypesJson);
        set => VehicleTypesJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    // === Location Keywords (JSON array). Empty = any route ===
    public string LocationKeywordsJson { get; set; } = "[]";

    [NotMapped]
    public List<string> LocationKeywords
    {
        get => DeserializeList(LocationKeywordsJson);
        set => LocationKeywordsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    private static List<string> DeserializeList(string json) =>
        string.IsNullOrEmpty(json)
            ? new List<string>()
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
}
