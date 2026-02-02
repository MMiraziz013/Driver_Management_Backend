using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.ReportPeriod;
using Clean.Application.Dtos.Responses;
using Clean.Application.Services.Report.AssignmentEngine;
using Clean.Application.Services.Report.Finalization;
using Clean.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;

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

    public ReportService(IUnitOfWork uow, IMapboxService mapboxService)
    {
        _uow = uow;
        _mapboxService = mapboxService;

        // Initialize sub-services
        _assignmentService = new DriverAssignmentService(uow);
        _finalizationService = new PeriodFinalizationService(uow);
        _uploadService = new TripUploadService(uow, mapboxService);
        _exportService = new ReportExportService(uow);
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
}