using System.ComponentModel.DataAnnotations;

namespace Clean.Domain.Entities;

public class Company
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Company name as it appears in uploaded reports
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Normalized name for matching (uppercase, trimmed)
    /// </summary>
    [MaxLength(255)]
    public string NormalizedName { get; set; } = string.Empty;
    
    /// <summary>
    /// Category this company belongs to
    /// </summary>
    public int? CompanyCategoryId { get; set; }
    public virtual CompanyCategory? Category { get; set; }
    
    /// <summary>
    /// Alternative names/aliases that should map to this company
    /// </summary>
    [MaxLength(1000)]
    public string? Aliases { get; set; }
    
    /// <summary>
    /// Notes about this company
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Whether this company is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// First seen in which report
    /// </summary>
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}