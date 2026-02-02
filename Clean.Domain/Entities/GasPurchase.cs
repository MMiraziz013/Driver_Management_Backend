using System.ComponentModel.DataAnnotations.Schema;

namespace Clean.Domain.Entities;

/// <summary>
/// Represents a single gas/fuel purchase record from the uploaded report
/// </summary>
public class GasPurchase
{
    public int Id { get; set; }

    /// <summary>
    /// The report period this purchase belongs to
    /// </summary>
    public int ReportPeriodId { get; set; }
    public ReportPeriod ReportPeriod { get; set; } = null!;

    /// <summary>
    /// Date of the gas purchase
    /// </summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// Amount of fuel purchased in liters
    /// </summary>
    public double LitersAmount { get; set; }

    /// <summary>
    /// Type of fuel: "АИ-92", "АИ-95", or "ДТ"
    /// </summary>
    public string FuelType { get; set; } = string.Empty;

    /// <summary>
    /// Cost of the purchase in UZS (Uzbeki Soums)
    /// </summary>
    public decimal AmountUzs { get; set; }

    /// <summary>
    /// Calculated price per liter (AmountUzs / LitersAmount)
    /// </summary>
    [NotMapped]
    public decimal PricePerLiter => LitersAmount > 0 
        ? Math.Round(AmountUzs / (decimal)LitersAmount, 2) 
        : 0;

    /// <summary>
    /// How much of this purchase has been allocated to vehicles
    /// </summary>
    public double AllocatedLiters { get; set; }

    /// <summary>
    /// Remaining unallocated liters
    /// </summary>
    [NotMapped]
    public double RemainingLiters => LitersAmount - AllocatedLiters;

    /// <summary>
    /// Whether this purchase has been fully allocated
    /// </summary>
    [NotMapped]
    public bool IsFullyAllocated => Math.Abs(RemainingLiters) < 0.01;

    /// <summary>
    /// Allocations of this purchase to vehicles
    /// </summary>
    public List<VehicleFuelAllocation> Allocations { get; set; } = [];

    /// <summary>
    /// Optional notes or reference number from the receipt
    /// </summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
