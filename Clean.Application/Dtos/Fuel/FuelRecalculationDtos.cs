namespace Clean.Application.Dtos.Fuel;

public class RecalculateFuelRequest
{
    /// <summary>
    /// Vehicles to recalculate. Empty = every vehicle whose daily tank level is over capacity or below zero.
    /// </summary>
    public List<int>? VehicleIds { get; set; }
}

public class FuelRecalculationResultDto
{
    public int ReportPeriodId { get; set; }
    public bool Saved { get; set; }
    public double LitersReleased { get; set; }
    public double LitersReallocated { get; set; }
    /// <summary>
    /// Liters of this period's purchases that are not allocated to any vehicle afterwards
    /// </summary>
    public double LitersLeftUnallocated { get; set; }
    public List<string> Warnings { get; set; } = [];
    public List<VehicleFuelRecalculationDto> Vehicles { get; set; } = [];
}

public class VehicleFuelRecalculationDto
{
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;

    /// <summary>
    /// Picked, Overflow or Deficit
    /// </summary>
    public string SelectedBecause { get; set; } = string.Empty;

    public double LitersBefore { get; set; }
    public double LitersAfter { get; set; }
    public decimal CostBefore { get; set; }
    public decimal CostAfter { get; set; }
    public double EndingLevelBefore { get; set; }
    public double EndingLevelAfter { get; set; }
    public List<FuelViolationDto> ViolationsBefore { get; set; } = [];
    public List<FuelViolationDto> ViolationsAfter { get; set; } = [];
}

public class FuelViolationDto
{
    public DateTime Date { get; set; }

    /// <summary>
    /// Overflow or Deficit
    /// </summary>
    public string Kind { get; set; } = string.Empty;
    public double Level { get; set; }
}
