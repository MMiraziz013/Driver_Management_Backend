using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.ReportPeriod;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Services.Report.Finalization;

/// <summary>
/// Orchestrates the complete period finalization process,
/// including both fuel and driver state finalization.
/// </summary>
public class PeriodFinalizationService
{
    private readonly IUnitOfWork _uow;
    private readonly FuelFinalizationService _fuelService;
    private readonly DriverFinalizationService _driverService;

    public PeriodFinalizationService(IUnitOfWork uow)
    {
        _uow = uow;
        _fuelService = new FuelFinalizationService();
        _driverService = new DriverFinalizationService();
    }

    /// <summary>
    /// Finalize an entire period - both fuel allocation and driver assignments.
    /// </summary>
    public async Task<Response<PeriodFinalizationResultDto>> FinalizePeriodAsync(int periodId, string? userId = null)
    {
        try
        {
            var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
            if (period == null)
            {
                return new Response<PeriodFinalizationResultDto>(
                    HttpStatusCode.NotFound,
                    "Report period not found"
                );
            }

            if (period.IsFinalized)
            {
                return new Response<PeriodFinalizationResultDto>(
                    HttpStatusCode.BadRequest,
                    $"Period already finalized on {period.FinalizedAt:yyyy-MM-dd HH:mm}"
                );
            }

            var result = new PeriodFinalizationResultDto
            {
                PeriodId = periodId,
                FinalizedAt = DateTime.UtcNow
            };

            // Get required data
            var vehicles = (await _uow.Vehicles.GetAllAsync()).ToList();
            var drivers = (await _uow.Drivers.GetActiveDriversWithDetailsAsync()).ToList();
            var allocations = (await _uow.FuelAllocations.GetByPeriodIdAsync(periodId)).ToList();

            // ===== 1. FINALIZE FUEL ALLOCATION =====
            Console.WriteLine($"\n=== FINALIZING FUEL FOR PERIOD {periodId} ===");
            result.FuelSummary = _fuelService.FinalizeFuelAllocation(period, vehicles, allocations);

            if (result.FuelSummary.VehiclesWithDeficit > 0)
            {
                result.Warnings.Add($"{result.FuelSummary.VehiclesWithDeficit} vehicle(s) have fuel deficit");
            }

            // ===== 2. FINALIZE DRIVER ASSIGNMENTS =====
            Console.WriteLine($"\n=== FINALIZING DRIVERS FOR PERIOD {periodId} ===");
            result.DriverSummary = await _driverService.FinalizeDriverAssignmentsAsync(
                period, drivers, _uow.DriverPeriodStates);

            if (result.DriverSummary.DriversWithWarnings > 0)
            {
                result.Warnings.Add($"{result.DriverSummary.DriversWithWarnings} driver(s) have warnings (hours/rest)");
            }

            // ===== 3. MARK PERIOD AS FINALIZED =====
            period.IsFinalized = true;
            period.FinalizedAt = DateTime.UtcNow;
            period.IsFuelFinalized = true;
            period.FuelFinalizedAt = DateTime.UtcNow;
            period.IsAssignmentFinalized = true;
            period.AssignmentFinalizedAt = DateTime.UtcNow;
            period.UpdatedAt = DateTime.UtcNow;

            await _uow.CompleteAsync();

            result.Success = true;
            result.Message = $"Period finalized successfully. " +
                            $"{result.FuelSummary.VehiclesUpdated} vehicles and {result.DriverSummary.DriversUpdated} drivers updated.";

            Console.WriteLine($"\n{'=',-60}");
            Console.WriteLine($"=== PERIOD {periodId} FULLY FINALIZED ===");
            Console.WriteLine($"Vehicles updated: {result.FuelSummary.VehiclesUpdated}");
            Console.WriteLine($"Drivers updated: {result.DriverSummary.DriversUpdated}");
            Console.WriteLine($"{'=',-60}\n");

            return new Response<PeriodFinalizationResultDto>(HttpStatusCode.OK, result.Message, result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Period finalization error: {ex.Message}");
            return new Response<PeriodFinalizationResultDto>(
                HttpStatusCode.InternalServerError,
                ex.Message
            );
        }
    }

    /// <summary>
    /// Preview unified finalization without saving.
    /// </summary>
    public async Task<Response<PeriodFinalizationResultDto>> PreviewPeriodFinalizationAsync(int periodId)
    {
        try
        {
            var period = await _uow.ReportPeriods.GetWithAssignmentsAsync(periodId);
            if (period == null)
            {
                return new Response<PeriodFinalizationResultDto>(
                    HttpStatusCode.NotFound,
                    "Report period not found"
                );
            }

            if (period.IsFinalized)
            {
                return new Response<PeriodFinalizationResultDto>(
                    HttpStatusCode.BadRequest,
                    $"Period already finalized on {period.FinalizedAt:yyyy-MM-dd HH:mm}"
                );
            }

            var result = new PeriodFinalizationResultDto
            {
                PeriodId = periodId,
                FinalizedAt = DateTime.UtcNow,
                IsPreview = true
            };

            var vehicles = (await _uow.Vehicles.GetAllAsync()).ToList();
            var drivers = (await _uow.Drivers.GetActiveDriversWithDetailsAsync()).ToList();
            var allocations = (await _uow.FuelAllocations.GetByPeriodIdAsync(periodId)).ToList();

            // Preview fuel finalization (doesn't save)
            result.FuelSummary = _fuelService.PreviewFuelFinalization(period, vehicles, allocations);

            // Preview driver finalization (doesn't save)
            result.DriverSummary = _driverService.PreviewDriverFinalization(period, drivers);

            // Collect warnings
            if (result.FuelSummary.VehiclesWithDeficit > 0)
            {
                result.Warnings.Add($"{result.FuelSummary.VehiclesWithDeficit} vehicle(s) have fuel deficit");
            }
            if (result.DriverSummary.DriversWithWarnings > 0)
            {
                result.Warnings.Add($"{result.DriverSummary.DriversWithWarnings} driver(s) have hour/rest warnings");
            }

            // Check for missing data
            if (!period.Trips.Any())
            {
                result.Warnings.Add("No trips found in this period");
            }

            if (!allocations.Any())
            {
                result.Warnings.Add("No fuel allocations found - run fuel allocation first");
            }

            var hasAssignments = period.Trips.Any(t => t.Assignments.Any(a => !a.HasConflict));
            if (!hasAssignments)
            {
                result.Warnings.Add("No driver assignments found - run assignment engine first");
            }

            result.Success = true;
            result.Message = "Preview generated. No changes have been saved.";

            return new Response<PeriodFinalizationResultDto>(HttpStatusCode.OK, result.Message, result);
        }
        catch (Exception ex)
        {
            return new Response<PeriodFinalizationResultDto>(
                HttpStatusCode.InternalServerError,
                ex.Message
            );
        }
    }

    /// <summary>
    /// Revert period finalization
    /// </summary>
    public async Task<Response<string>> RevertPeriodFinalizationAsync(int periodId)
    {
        try
        {
            var period = await _uow.ReportPeriods.GetByIdAsync(periodId);
            if (period == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, "Report period not found");
            }

            if (!period.IsFinalized)
            {
                return new Response<string>(HttpStatusCode.BadRequest, "Period is not finalized");
            }

            // Unlock the period
            period.IsFinalized = false;
            period.FinalizedAt = null;
            period.IsFuelFinalized = false;
            period.FuelFinalizedAt = null;
            period.IsAssignmentFinalized = false;
            period.AssignmentFinalizedAt = null;
            period.UpdatedAt = DateTime.UtcNow;

            await _uow.CompleteAsync();

            return new Response<string>(
                HttpStatusCode.OK,
                "Period finalization reverted. Note: Vehicle fuel levels and driver states may need manual correction."
            );
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}