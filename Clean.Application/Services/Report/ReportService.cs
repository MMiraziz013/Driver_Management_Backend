using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Report;
using Clean.Application.Dtos.ReportPeriod;
using Clean.Application.Dtos.Responses;
using Clean.Application.Services.Report.AssignmentEngine;
using Clean.Application.Services.Report.Finalization;
using Clean.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clean.Application.Services.Report;

/// <summary>
/// Main report service that orchestrates operations.
/// Delegates specialized work to focused sub-services.
/// </summary>
public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapboxService _mapboxService;

    // Sub-services
    private readonly DriverAssignmentService _assignmentService;
    private readonly PeriodFinalizationService _finalizationService;
    private readonly TripUploadService _uploadService;
    private readonly ReportExportService _exportService;
    private readonly WaybillExportService _waybillExportService;
    public ReportService(IUnitOfWork uow, IMapboxService mapboxService)
    {
        _uow = uow;
        _mapboxService = mapboxService;

        // Initialize sub-services
        _assignmentService = new DriverAssignmentService(uow);
        _finalizationService = new PeriodFinalizationService(uow);
        _uploadService = new TripUploadService(uow, mapboxService);
        _exportService = new ReportExportService(uow);
        _waybillExportService = new WaybillExportService(uow);
    }

    // =========================================================================
    // PERIOD MANAGEMENT
    // =========================================================================

    public async Task<Response<List<GetReportPeriodDto>>> GetAllPeriods()
    {
        try
        {
            var allPeriods = await _uow.ReportPeriods.GetAllAsync();
            var all = allPeriods.Select(p => new GetReportPeriodDto
            {
                Id = p.Id,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Description = p.Description,
                GeneratedAt = p.GeneratedAt,
                GeneratedBy = p.GeneratedBy,
                Status = ReportStatus.Finalized,
                IsFinalized = p.IsFinalized,
                FinalizedAt = p.FinalizedAt,
                IsFuelFinalized = p.IsFuelFinalized,
                FuelFinalizedAt = p.FuelFinalizedAt,
                IsAssignmentFinalized = p.IsAssignmentFinalized,
                AssignmentFinalizedAt = p.AssignmentFinalizedAt
            }).ToList();

            return new Response<List<GetReportPeriodDto>>(HttpStatusCode.OK, all);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    // =========================================================================
    // TRIP UPLOAD - Delegated to TripUploadService
    // =========================================================================

    public Task<Response<string>> UploadReportAsync(IFormFile file, int periodId)
    {
        return _uploadService.UploadReportAsync(file, periodId);
    }

    // =========================================================================
    // AUTO-ASSIGNMENT - Delegated to DriverAssignmentService
    // =========================================================================

    public Task<Response<string>> RunAutoAssignmentAsync(int periodId)
    {
        // Use carryover=false for now; can be enabled when infrastructure is ready
        return _assignmentService.RunAutoAssignmentAsync(periodId, useCarryover: false);
    }

    /// <summary>
    /// Run auto-assignment with carryover from previous period
    /// </summary>
    public Task<Response<string>> RunAutoAssignmentWithCarryoverAsync(int periodId)
    {
        return _assignmentService.RunAutoAssignmentAsync(periodId, useCarryover: true);
    }

    // =========================================================================
    // EXPORT - Delegated to ReportExportService
    // =========================================================================

    public Task<byte[]> ExportReportAsync(int periodId)
    {
        return _exportService.ExportReportAsync(periodId);
    }
    
    // =========================================================================
    // FINALIZATION - Delegated to PeriodFinalizationService
    // =========================================================================

    public Task<Response<PeriodFinalizationResultDto>> FinalizePeriodAsync(int periodId, string? userId = null)
    {
        return _finalizationService.FinalizePeriodAsync(periodId, userId);
    }

    public Task<Response<PeriodFinalizationResultDto>> PreviewPeriodFinalizationAsync(int periodId)
    {
        return _finalizationService.PreviewPeriodFinalizationAsync(periodId);
    }

    public Task<Response<string>> RevertPeriodFinalizationAsync(int periodId)
    {
        return _finalizationService.RevertPeriodFinalizationAsync(periodId);
    }
    
    
    // =========================================================================
    // JOURNEY SERVICES - Waybill reporting features
    // =========================================================================
    public async Task<Response<byte[]>> GetWaybillReportAsync(int periodId)
    {
        var result = await _waybillExportService.ExportWaybillReportAsync(periodId);
        return new Response<byte[]>(HttpStatusCode.OK, result);
    }

    public async Task<Response<List<JourneyDto>>> GetAllJourneysAsync(int periodId)    
    {
        var result = await _waybillExportService.GetJourneysAsync(periodId);
        return new Response<List<JourneyDto>>(HttpStatusCode.OK, result);
    }
    
    /// <summary>
    /// Update a vehicle's current mileage
    /// </summary>
    public async Task<Response<string>> UpdateVehicleMileageAsync(int vehicleId, double newMileage)
    {
        try
        {
            var vehicle = await _uow.Vehicles.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, "Vehicle not found");
            }

            if (newMileage < vehicle.CurrentMileage)
            {
                return new Response<string>(HttpStatusCode.BadRequest, 
                    $"New mileage ({newMileage}) cannot be less than current mileage ({vehicle.CurrentMileage})");
            }

            vehicle.CurrentMileage = newMileage;
            vehicle.MileageUpdatedAt = DateTime.UtcNow;
            vehicle.UpdatedAt = DateTime.UtcNow;

            await _uow.CompleteAsync();

            return new Response<string>(HttpStatusCode.OK, 
                $"Vehicle {vehicle.PlateNumber} mileage updated to {newMileage} km");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Bulk update vehicle mileages
    /// </summary>
    public async Task<Response<string>> BulkUpdateVehicleMileagesAsync(List<(int VehicleId, double NewMileage)> updates)
    {
        try
        {
            var vehicles = await _uow.Vehicles.GetAllAsync();
            var vehicleDict = vehicles.ToDictionary(v => v.Id);
            var errors = new List<string>();
            var updated = 0;

            foreach (var (vehicleId, newMileage) in updates)
            {
                if (!vehicleDict.TryGetValue(vehicleId, out var vehicle))
                {
                    errors.Add($"Vehicle ID {vehicleId} not found");
                    continue;
                }

                if (newMileage < vehicle.CurrentMileage)
                {
                    errors.Add(
                        $"Vehicle {vehicle.PlateNumber}: new mileage ({newMileage}) < current ({vehicle.CurrentMileage})");
                    continue;
                }

                vehicle.CurrentMileage = newMileage;
                vehicle.MileageUpdatedAt = DateTime.UtcNow;
                vehicle.UpdatedAt = DateTime.UtcNow;
                updated++;
            }

            await _uow.CompleteAsync();

            var message = $"Updated {updated} vehicle(s)";
            if (errors.Any())
            {
                message += $". Errors: {string.Join("; ", errors)}";
                return new Response<string>(HttpStatusCode.OK, message); // Partial success
            }

            return new Response<string>(HttpStatusCode.OK, message);
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, ex.Message);
        }

    }
}