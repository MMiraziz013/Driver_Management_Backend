using System.ComponentModel.DataAnnotations;

namespace Clean.Domain.Entities;

public class AccountingReport
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Year of the report data
    /// </summary>
    public int Year { get; set; }
    
    /// <summary>
    /// Month of the report data (1-12)
    /// </summary>
    public int Month { get; set; }
    
    /// <summary>
    /// Original filename
    /// </summary>
    [MaxLength(255)]
    public string? FileName { get; set; }
    
    /// <summary>
    /// Number of transactions imported
    /// </summary>
    public int TransactionCount { get; set; }
    
    /// <summary>
    /// Total amount in this report
    /// </summary>
    public decimal TotalAmount { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string? UploadedBy { get; set; }
    
    // Navigation
    public virtual ICollection<AccountingTransaction> Transactions { get; set; } = new List<AccountingTransaction>();
}