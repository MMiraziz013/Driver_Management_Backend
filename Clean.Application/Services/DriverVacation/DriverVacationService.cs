// Clean.Application/Services/Driver/DriverVacationService.cs

using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.DriverVacation;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Services.DriverVacation;

public class DriverVacationService : IDriverVacationService
{
    private readonly IDriverVacationRepository _vacationRepository;
    private readonly IDriverRepository _driverRepository;

    public DriverVacationService(
        IDriverVacationRepository vacationRepository,
        IDriverRepository driverRepository)
    {
        _vacationRepository = vacationRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Response<GetDriverVacationDto>> AddVacationAsync(AddDriverVacationDto dto)
    {
        // Validate driver exists
        var driver = await _driverRepository.GetDriverByIdAsync(dto.DriverId);
        if (driver == null)
        {
            return new Response<GetDriverVacationDto>(HttpStatusCode.NotFound, 
                $"Driver with ID {dto.DriverId} not found");
        }

        // Validate dates
        if (dto.EndDate < dto.StartDate)
        {
            return new Response<GetDriverVacationDto>(HttpStatusCode.BadRequest, 
                "End date cannot be before start date");
        }

        // Check for overlapping vacations
        var hasOverlap = await _vacationRepository.HasOverlappingVacationAsync(
            dto.DriverId, dto.StartDate, dto.EndDate);
        
        if (hasOverlap)
        {
            return new Response<GetDriverVacationDto>(HttpStatusCode.Conflict, 
                "Driver already has a vacation scheduled during this period");
        }

        var vacation = new Domain.Entities.DriverVacation
        {
            DriverId = dto.DriverId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            Notes = dto.Notes
        };

        var created = await _vacationRepository.AddAsync(vacation);
        
        return new Response<GetDriverVacationDto>(HttpStatusCode.Created, 
            MapToDto(created, driver.FullName));
    }

    public async Task<Response<GetDriverVacationDto?>> GetByIdAsync(int id)
    {
        var vacation = await _vacationRepository.GetByIdAsync(id);
        if (vacation == null)
        {
            return new Response<GetDriverVacationDto?>(HttpStatusCode.NotFound, 
                $"Vacation with ID {id} not found");
        }

        return new Response<GetDriverVacationDto?>(HttpStatusCode.OK, 
            MapToDto(vacation, vacation.Driver.FullName));
    }

    public async Task<Response<IEnumerable<GetDriverVacationDto>>> GetByDriverIdAsync(int driverId)
    {
        var driver = await _driverRepository.GetDriverByIdAsync(driverId);
        if (driver == null)
        {
            return new Response<IEnumerable<GetDriverVacationDto>>(HttpStatusCode.NotFound, 
                $"Driver with ID {driverId} not found");
        }

        var vacations = await _vacationRepository.GetByDriverIdAsync(driverId);
        var dtos = vacations.Select(v => MapToDto(v, driver.FullName));

        return new Response<IEnumerable<GetDriverVacationDto>>(HttpStatusCode.OK, dtos);
    }

    public async Task<Response<IEnumerable<GetDriverVacationDto>>> GetAllAsync()
    {
        var vacations = await _vacationRepository.GetAllAsync();
        var dtos = vacations.Select(v => MapToDto(v, v.Driver.FullName));

        return new Response<IEnumerable<GetDriverVacationDto>>(HttpStatusCode.OK, dtos);
    }

    public async Task<Response<IEnumerable<GetDriverVacationDto>>> GetActiveVacationsAsync()
    {
        var vacations = await _vacationRepository.GetActiveVacationsAsync(DateTime.UtcNow);
        var dtos = vacations.Select(v => MapToDto(v, v.Driver.FullName));

        return new Response<IEnumerable<GetDriverVacationDto>>(HttpStatusCode.OK, dtos);
    }

    public async Task<Response<IEnumerable<GetDriverVacationDto>>> GetVacationsInRangeAsync(
        DateTime startDate, DateTime endDate)
    {
        var vacations = await _vacationRepository.GetVacationsInRangeAsync(startDate, endDate);
        var dtos = vacations.Select(v => MapToDto(v, v.Driver.FullName));

        return new Response<IEnumerable<GetDriverVacationDto>>(HttpStatusCode.OK, dtos);
    }

    public async Task<Response<GetDriverVacationDto?>> UpdateVacationAsync(UpdateDriverVacationDto dto)
    {
        var vacation = await _vacationRepository.GetByIdAsync(dto.Id);
        if (vacation == null)
        {
            return new Response<GetDriverVacationDto?>(HttpStatusCode.NotFound, 
                $"Vacation with ID {dto.Id} not found");
        }

        // Update fields if provided
        if (dto.StartDate.HasValue)
            vacation.StartDate = dto.StartDate.Value.Date;
        
        if (dto.EndDate.HasValue)
            vacation.EndDate = dto.EndDate.Value.Date;

        if (dto.Notes != null)
            vacation.Notes = dto.Notes;

        // Validate dates after update
        if (vacation.EndDate < vacation.StartDate)
        {
            return new Response<GetDriverVacationDto?>(HttpStatusCode.BadRequest, 
                "End date cannot be before start date");
        }

        // Check for overlapping vacations (excluding this one)
        var hasOverlap = await _vacationRepository.HasOverlappingVacationAsync(
            vacation.DriverId, vacation.StartDate, vacation.EndDate, dto.Id);
        
        if (hasOverlap)
        {
            return new Response<GetDriverVacationDto?>(HttpStatusCode.Conflict, 
                "This would overlap with another vacation for this driver");
        }

        var updated = await _vacationRepository.UpdateAsync(vacation);
        
        return new Response<GetDriverVacationDto?>(HttpStatusCode.OK, 
            MapToDto(updated!, vacation.Driver.FullName));
    }

    public async Task<Response<bool>> DeleteVacationAsync(int id)
    {
        var exists = await _vacationRepository.GetByIdAsync(id);
        if (exists == null)
        {
            return new Response<bool>(HttpStatusCode.NotFound, 
                $"Vacation with ID {id} not found");
        }

        var deleted = await _vacationRepository.DeleteAsync(id);
        return new Response<bool>(HttpStatusCode.OK, deleted);
    }

    public async Task<Response<bool>> IsDriverOnVacationAsync(int driverId, DateTime? date = null)
    {
        var checkDate = date ?? DateTime.UtcNow;
        var isOnVacation = await _vacationRepository.IsDriverOnVacationAsync(driverId, checkDate);
        return new Response<bool>(HttpStatusCode.OK, isOnVacation);
    }

    private static GetDriverVacationDto MapToDto(Domain.Entities.DriverVacation vacation, string driverName)
    {
        var today = DateTime.UtcNow.Date;
        var startDate = vacation.StartDate.Date;
        var endDate = vacation.EndDate.Date;

        return new GetDriverVacationDto
        {
            Id = vacation.Id,
            DriverId = vacation.DriverId,
            DriverName = driverName,
            StartDate = vacation.StartDate,
            EndDate = vacation.EndDate,
            Notes = vacation.Notes,
            IsActive = startDate <= today && endDate >= today,
            IsPast = endDate < today,
            IsFuture = startDate > today
        };
    }
}