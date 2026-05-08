using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Clean.Domain.Entities;

public class ExchangeRate
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// The year this exchange rate applies to
    /// </summary>
    public int Year { get; set; }
    
    /// <summary>
    /// USD to UZS exchange rate
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Rate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}