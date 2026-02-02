namespace Clean.Application.Dtos.Fuel;

// ===== REQUEST DTOs =====

/// <summary>
/// DTO for manually allocating fuel to a vehicle
/// </summary>
public class ManualFuelAllocationRequest
{
    public int GasPurchaseId { get; set; }
    public int VehicleId { get; set; }
    public double LitersToAllocate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for setting initial fuel levels for vehicles
/// </summary>
public class SetInitialFuelLevelRequest
{
    public int VehicleId { get; set; }
    public double InitialLiters { get; set; }
}

/// <summary>
/// DTO for updating vehicle fuel configuration
/// </summary>
public class UpdateVehicleFuelConfigRequest
{
    public int VehicleId { get; set; }
    public double FuelTankCapacity { get; set; }
    public double FuelConsumptionPer100Km { get; set; }
    public string FuelType { get; set; } = string.Empty;
    public double? InitialFuelLevel { get; set; }
}

// ===== RESPONSE DTOs =====

/// <summary>
/// Summary of gas purchases for a period
/// </summary>
public class GasPurchaseSummaryDto
{
    public int ReportPeriodId { get; set; }
    public int TotalPurchases { get; set; }
    public double TotalLitersPurchased { get; set; }
    public decimal TotalAmountUzs { get; set; }
    public double TotalLitersAllocated { get; set; }
    public double TotalLitersRemaining { get; set; }
    public List<FuelTypeSummaryDto> ByFuelType { get; set; } = [];
    public List<string> Messages { get; set; } = [];
}

/// <summary>
/// Summary per fuel type
/// </summary>
public class FuelTypeSummaryDto
{
    public string FuelType { get; set; } = string.Empty;
    public int PurchaseCount { get; set; }
    public double TotalLiters { get; set; }
    public decimal TotalAmountUzs { get; set; }
    public decimal AveragePricePerLiter { get; set; }
    public double AllocatedLiters { get; set; }
    public double RemainingLiters { get; set; }
}

/// <summary>
/// Individual gas purchase record
/// </summary>
public class GasPurchaseDto
{
    public int Id { get; set; }
    public DateTime PurchaseDate { get; set; }
    public double LitersAmount { get; set; }
    public string FuelType { get; set; } = string.Empty;
    public decimal AmountUzs { get; set; }
    public decimal PricePerLiter { get; set; }
    public double AllocatedLiters { get; set; }
    public double RemainingLiters { get; set; }
    public bool IsFullyAllocated { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Vehicle fuel status for a period
/// </summary>
public class VehicleFuelStatusDto
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string FuelType { get; set; } = string.Empty;
    public double TankCapacity { get; set; }
    public double ConsumptionPer100Km { get; set; }

    // Period-specific data
    public double InitialFuelLevel { get; set; }
    public double TotalDistanceDriven { get; set; }
    public double FuelConsumed { get; set; }
    public double FuelAllocated { get; set; }
    public double CurrentFuelLevel { get; set; }
    public decimal TotalFuelCostUzs { get; set; }

    // Status indicators
    public string Status { get; set; } = string.Empty; // "OK", "LOW", "OVER_CAPACITY", "NEGATIVE", "NOT_CONFIGURED"
    public List<string> Warnings { get; set; } = [];

    // Detailed breakdown
    public List<FuelAllocationDetailDto> Allocations { get; set; } = [];
}

/// <summary>
/// Detailed fuel allocation record
/// </summary>
public class FuelAllocationDetailDto
{
    public int Id { get; set; }
    public DateTime AllocationDate { get; set; }
    public double LitersAllocated { get; set; }
    public decimal CostUzs { get; set; }
    public string FuelType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? TripConfNumber { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Overall fuel calculation result for a period
/// </summary>
public class FuelCalculationResultDto
{
    public int ReportPeriodId { get; set; }
    public DateTime CalculatedAt { get; set; }
    public bool Success { get; set; }
    public List<string> Messages { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];

    // Summary statistics
    public double TotalDistanceDriven { get; set; }
    public double TotalFuelConsumed { get; set; }
    public double TotalFuelPurchased { get; set; }
    public double TotalFuelAllocated { get; set; }
    public double UnallocatedFuel { get; set; }
    public decimal TotalCostUzs { get; set; }

    // Per-vehicle results
    public List<VehicleFuelStatusDto> VehicleStatuses { get; set; } = [];

    // Validation results
    public int VehiclesWithIssues { get; set; }
    public int VehiclesOk { get; set; }
}

/// <summary>
/// DTO for the fuel balance simulation/preview
/// </summary>
public class FuelBalancePreviewDto
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public List<DailyFuelBalanceDto> DailyBalances { get; set; } = [];
}

/// <summary>
/// Daily fuel balance for a vehicle
/// </summary>
public class DailyFuelBalanceDto
{
    public DateTime Date { get; set; }
    public double StartingBalance { get; set; }
    public double FuelUsed { get; set; }
    public double FuelAdded { get; set; }
    public double EndingBalance { get; set; }
    public double DistanceDriven { get; set; }
    public List<string> TripConfNumbers { get; set; } = [];
    public bool HasWarning { get; set; }
    public string? WarningMessage { get; set; }
}


// Add these to your FuelDtos.cs file

public class FuelDiagnosticDto
{
    public int ReportPeriodId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // Vehicle consumption grouped by fuel type
    public List<FuelTypeConsumptionDto> VehicleConsumptionByFuelType { get; set; } = [];

    // Purchases grouped by fuel type
    public List<FuelTypePurchaseDto> PurchasesByFuelType { get; set; } = [];

    // Balance per fuel type
    public List<FuelTypeBalanceDto> BalanceByFuelType { get; set; } = [];

    // Overall totals
    public double TotalDistanceDriven { get; set; }
    public double TotalFuelNeeded { get; set; }
    public double TotalFuelPurchased { get; set; }
    public double OverallBalance { get; set; }
}

public class FuelTypeConsumptionDto
{
    public string FuelType { get; set; } = string.Empty;
    public int VehicleCount { get; set; }
    public int VehiclesWithTrips { get; set; }
    public double TotalDistanceKm { get; set; }
    public double TotalFuelNeeded { get; set; }
}

public class FuelTypePurchaseDto
{
    public string FuelType { get; set; } = string.Empty;
    public int PurchaseCount { get; set; }
    public double TotalLiters { get; set; }
    public decimal TotalCostUzs { get; set; }
}

public class FuelTypeBalanceDto
{
    public string FuelType { get; set; } = string.Empty;
    public double TotalPurchased { get; set; }
    public double TotalNeeded { get; set; }
    public double Balance { get; set; }
    public string Status { get; set; } = string.Empty; // "SURPLUS", "DEFICIT", "NO_VEHICLES"
}

public class FuelAllocationExportDto
{
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}

public class FuelFinalizationResultDto
{
    public int PeriodId { get; set; }
    public DateTime FinalizedAt { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsPreview { get; set; } = false;
    public int VehiclesWithDeficit { get; set; }
    public List<VehicleFuelUpdateDto> VehicleUpdates { get; set; } = new();
}

public class VehicleFuelUpdateDto
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public double PreviousInitialLevel { get; set; }
    public double FuelAllocated { get; set; }
    public double FuelConsumed { get; set; }
    public double CalculatedFinalLevel { get; set; }
    public double NewInitialLevel { get; set; }
    public bool HasDeficit { get; set; }
}

