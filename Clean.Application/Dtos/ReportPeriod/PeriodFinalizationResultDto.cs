using Clean.Application.Dtos.Driver;
using Clean.Application.Dtos.Fuel;
using Clean.Application.Services.Report.Finalization;

namespace Clean.Application.Dtos.ReportPeriod;

// ============================================================================
// UNIFIED PERIOD FINALIZATION
// ============================================================================
// This combines fuel allocation and driver assignment finalization into
// a single "Finalize Period" action that:
// 1. Finalizes fuel allocation → Updates vehicle initial fuel levels
// 2. Finalizes driver assignments → Updates driver states (hours, rest, etc.)
// 3. Marks the entire period as locked
// ============================================================================

public class PeriodFinalizationResultDto
{
    public int PeriodId { get; set; }
    public DateTime FinalizedAt { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsPreview { get; set; } = false;

    // Fuel finalization results
    public FuelFinalizationSummary? FuelSummary { get; set; }

    // Driver finalization results  
    public DriverFinalizationSummary? DriverSummary { get; set; }
    
    /// <summary>
    /// Vehicle mileage finalization summary
    /// </summary>
    public MileageFinalizationSummary? MileageSummary { get; set; }


    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}

public class FuelFinalizationSummary
{
    public int VehiclesUpdated { get; set; }
    public int VehiclesWithDeficit { get; set; }
    public double TotalFuelAllocated { get; set; }
    public double TotalFuelConsumed { get; set; }
    public List<VehicleFuelUpdateDto> VehicleUpdates { get; set; } = [];
}

public class DriverFinalizationSummary
{
    public int DriversUpdated { get; set; }
    public int DriversWithWarnings { get; set; }
    public int TotalTripsAssigned { get; set; }
    public double TotalHoursWorked { get; set; }
    public List<DriverStateDto> DriverStates { get; set; } = [];
}
