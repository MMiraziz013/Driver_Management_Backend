using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Enums;

namespace Clean.Application.Services.ReportPeriod;

public class ReportPeriodService : IReportPeriodService
{
    private readonly IUnitOfWork _uow;

    public ReportPeriodService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<Domain.Entities.ReportPeriod>> CreatePeriodAsync(string description, DateTime start, DateTime end)
    {
        try
        {
            var period = new Domain.Entities.ReportPeriod
            {
                Description = description,
                // Specify that the incoming dates should be treated as UTC
                StartDate = DateTime.SpecifyKind(start, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(end, DateTimeKind.Utc),
            
                Status = ReportStatus.Draft,
                GeneratedAt = DateTime.UtcNow, // UtcNow already has Kind=Utc
                GeneratedBy = "System Admin"
            };

            await _uow.ReportPeriods.AddAsync(period);
            await _uow.CompleteAsync();

            return new Response<Domain.Entities.ReportPeriod>(HttpStatusCode.Created, "Report period created.", period);
        }
        catch (Exception ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            return new Response<Domain.Entities.ReportPeriod>(HttpStatusCode.InternalServerError, new List<string> { message });
        }
    }

    public async Task<Response<List<Domain.Entities.ReportPeriod>>> GetAllPeriodsAsync()
    {
        var periods = await _uow.ReportPeriods.GetAllAsync();
        return new Response<List<Domain.Entities.ReportPeriod>>(HttpStatusCode.OK, periods);
    }

    public async Task<Response<Domain.Entities.ReportPeriod>> GetPeriodByIdAsync(int id)
    {
        var period = await _uow.ReportPeriods.GetByIdAsync(id);
        if (period == null)
            return new Response<Domain.Entities.ReportPeriod>(HttpStatusCode.NotFound, "Period not found.");

        return new Response<Domain.Entities.ReportPeriod>(HttpStatusCode.OK, period);
    }
    
    public async Task<Response<string>> DeletePeriodWithTripsAsync(int periodId)
    {
        try
        {
            var period = await _uow.ReportPeriods.GetByIdAsync(periodId);
            if (period == null)
            {
                return new Response<string>(HttpStatusCode.NotFound, 
                    new List<string> { "Report period not found" });
            }

            // Prevent deletion of finalized periods
            if (period.IsFinalized || period.IsFuelFinalized || period.IsAssignmentFinalized)
            {
                return new Response<string>(HttpStatusCode.BadRequest, 
                    new List<string> { "Cannot delete a finalized period. Revert finalization first." });
            }

            // Get all trips for this period
            var allTrips = await _uow.Trips.GetAllAsync();
            var periodTrips = allTrips.Where(t => t.ReportPeriodId == periodId).ToList();
            int tripCount = periodTrips.Count;

            // Delete trips first (due to foreign key constraints)
            foreach (var trip in periodTrips)
            {
                _uow.Trips.Delete(trip);
            }

            // Delete the period
            _uow.ReportPeriods.Delete(period);
            await _uow.CompleteAsync();

            return new Response<string>(HttpStatusCode.OK, 
                $"Deleted period '{period.Description}' and {tripCount} associated trips");
        }
        catch (Exception ex)
        {
            return new Response<string>(HttpStatusCode.InternalServerError, 
                new List<string> { ex.Message });
        }
    }}