using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Clean.Domain.Enums;

namespace Clean.Domain.Entities;

public class AccountingTransaction
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Reference to the uploaded report
    /// </summary>
    public int AccountingReportId { get; set; }
    
    /// <summary>
    /// Year of the transaction
    /// </summary>
    public int Year { get; set; }
    
    /// <summary>
    /// Month of the transaction (1-12)
    /// </summary>
    public int Month { get; set; }
    
    /// <summary>
    /// INH (In-House) or FOT (Farm Out)
    /// </summary>
    public TransactionType Type { get; set; }
    
    /// <summary>
    /// Affiliate first name (for FOT)
    /// </summary>
    [MaxLength(100)]
    public string? AffiliateFirstName { get; set; }
    
    /// <summary>
    /// Affiliate last name (for FOT)
    /// </summary>
    [MaxLength(100)]
    public string? AffiliateLastName { get; set; }
    
    /// <summary>
    /// Full affiliate name (computed)
    /// </summary>
    [NotMapped]
    public string AffiliateName => $"{AffiliateFirstName} {AffiliateLastName}".Trim();
    
    /// <summary>
    /// Billing contact name
    /// </summary>
    [MaxLength(200)]
    public string? BillingContact { get; set; }
    
    /// <summary>
    /// Booking contact name
    /// </summary>
    [MaxLength(200)]
    public string? BookingContact { get; set; }
    
    /// <summary>
    /// Passenger first name
    /// </summary>
    [MaxLength(100)]
    public string? PassengerFirstName { get; set; }
    
    /// <summary>
    /// Company name
    /// </summary>
    [MaxLength(200)]
    public string? Company { get; set; }
    
    /// <summary>
    /// Car/Vehicle identifier (plate or name)
    /// </summary>
    [MaxLength(100)]
    public string? Car { get; set; }
    
    /// <summary>
    /// Vehicle type (e.g., Sedan, SUV, Van)
    /// </summary>
    [MaxLength(100)]
    public string? VehicleType { get; set; }
    
    /// <summary>
    /// Service type (e.g., Transfer, Round Trip)
    /// </summary>
    [MaxLength(100)]
    public string? ServiceType { get; set; }
    
    /// <summary>
    /// Transaction status
    /// </summary>
    [MaxLength(50)]
    public string? Status { get; set; }
    
    /// <summary>
    /// Payment method
    /// </summary>
    [MaxLength(50)]
    public string? PmtMethod { get; set; }
    
    /// <summary>
    /// Trip total amount (in USD)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TripTotal { get; set; }
    
    // Navigation
    public virtual AccountingReport? AccountingReport { get; set; }
}