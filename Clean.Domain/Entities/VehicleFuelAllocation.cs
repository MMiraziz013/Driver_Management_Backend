namespace Clean.Domain.Entities;

/// <summary>
/// Tracks the allocation of gas purchases to specific vehicles
/// Links a GasPurchase to a Vehicle with the allocated amount
/// </summary>
public class VehicleFuelAllocation
{
    public int Id { get; set; }

    /// <summary>
    /// The gas purchase this allocation comes from
    /// </summary>
    public int GasPurchaseId { get; set; }
    public GasPurchase GasPurchase { get; set; } = null!;

    /// <summary>
    /// The vehicle receiving this fuel allocation
    /// </summary>
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    /// <summary>
    /// The report period this allocation belongs to
    /// </summary>
    public int ReportPeriodId { get; set; }
    public ReportPeriod ReportPeriod { get; set; } = null!;

    /// <summary>
    /// Amount of fuel allocated in liters
    /// </summary>
    public double LitersAllocated { get; set; }

    /// <summary>
    /// Cost of this allocation in UZS
    /// Calculated as: (LitersAllocated / GasPurchase.LitersAmount) * GasPurchase.AmountUzs
    /// </summary>
    public decimal AllocationCostUzs { get; set; }

    /// <summary>
    /// Date when this allocation was made (for tracking purposes)
    /// </summary>
    public DateTime AllocationDate { get; set; }

    /// <summary>
    /// Type of allocation for auditing
    /// </summary>
    public FuelAllocationReason Reason { get; set; }

    /// <summary>
    /// Optional reference to the trip that consumed this fuel
    /// </summary>
    public int? TripId { get; set; }
    public Trip? Trip { get; set; }

    /// <summary>
    /// Notes explaining why this allocation was made
    /// </summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Reason for fuel allocation - helps with auditing and reports
/// </summary>
public enum FuelAllocationReason
{
    AutoDistanceBased = 0,
    ManualAllocation = 1,
    TripConsumption = 2,
    OperationalOverhead = 3,
    TankFill = 4,              // ADD THIS
    VehicleWarmup = 5,
    GarageMovement = 6,
    TrafficAdjustment = 7,
    BalanceAdjustment = 8,
    InitialFillUp = 9,
    PeriodCarryOver = 10
}