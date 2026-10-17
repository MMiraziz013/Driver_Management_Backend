using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.VehicleType;
using Microsoft.EntityFrameworkCore;

namespace Clean.Application.Services.VehicleType;

public class VehicleTypeService : IVehicleTypeService
{
    private readonly IVehicleTypeRepository _vehicleTypeRepository;
    private readonly IUnitOfWork _uow;

    public VehicleTypeService(IVehicleTypeRepository vehicleTypeRepository, IUnitOfWork uow)
    {
        _vehicleTypeRepository = vehicleTypeRepository;
        _uow = uow;
    }
    
    
    public async Task<PaginatedResponse<GetVehicleTypeDto>> GetVehicleTypesAsync(
        PaginationFilter filter)
    {
        
        // Setting the default page size to 20
        if (filter.PageSize == 10)
        {
            filter.PageSize = 20;
        }
        var query = _vehicleTypeRepository.Query();

        var totalRecords = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new GetVehicleTypeDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description
            })
            .ToListAsync();

        return new PaginatedResponse<GetVehicleTypeDto>(
            items,
            filter.PageNumber,
            filter.PageSize,
            totalRecords
        );
    }

    
    public async Task<Response<GetVehicleTypeDto>> AddVehicleTypeAsync(AddVehicleTypeDto dto)
    {
        try
        {
            var vehicleType = new Domain.Entities.VehicleType
            {
                Name = dto.Name,
                Capacity = dto.Capacity,
                Description = dto.Description,
            };

            await _uow.VehicleTypes.AddAsync(vehicleType);
            await _uow.CompleteAsync();

            var result = new GetVehicleTypeDto { Id = vehicleType.Id, Name = vehicleType.Name, Description = vehicleType.Description};
            return new Response<GetVehicleTypeDto>(HttpStatusCode.Created, "Vehicle Type added.", result);
        }
        catch (Exception ex)
        {
            return new Response<GetVehicleTypeDto>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }

    public async Task<Response<GetVehicleTypeDto>> UpdateVehicleTypeAsync(int id, UpdateVehicleTypeDto dto)
    {
        try
        {
            var vehicleType = await _vehicleTypeRepository.GetByIdAsync(id);

            if (vehicleType == null)
            {
                return new Response<GetVehicleTypeDto>(HttpStatusCode.NotFound,
                    new List<string> { $"Vehicle Type with ID {id} not found" });
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                vehicleType.Name = dto.Name;
            }

            if (dto.Description != null)
            {
                vehicleType.Description = dto.Description;
            }

            if (dto.Capacity.HasValue)
            {
                vehicleType.Capacity = dto.Capacity.Value;
            }

            await _uow.CompleteAsync();

            var result = new GetVehicleTypeDto
            {
                Id = vehicleType.Id,
                Name = vehicleType.Name,
                Description = vehicleType.Description
            };

            return new Response<GetVehicleTypeDto>(HttpStatusCode.OK, "Vehicle Type updated.", result);
        }
        catch (Exception ex)
        {
            return new Response<GetVehicleTypeDto>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<List<VehicleTypeFallbackDto>>> GetFallbacksAsync(int id)
    {
        try
        {
            if (!await _uow.Context.VehicleTypes.AnyAsync(vt => vt.Id == id))
            {
                return new Response<List<VehicleTypeFallbackDto>>(HttpStatusCode.NotFound,
                    new List<string> { $"Vehicle Type with ID {id} not found" });
            }

            return new Response<List<VehicleTypeFallbackDto>>(HttpStatusCode.OK, await LoadFallbacksAsync(id));
        }
        catch (Exception ex)
        {
            return new Response<List<VehicleTypeFallbackDto>>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    public async Task<Response<List<VehicleTypeFallbackDto>>> UpdateFallbacksAsync(int id, UpdateVehicleTypeFallbacksDto dto)
    {
        try
        {
            if (!await _uow.Context.VehicleTypes.AnyAsync(vt => vt.Id == id))
            {
                return new Response<List<VehicleTypeFallbackDto>>(HttpStatusCode.NotFound,
                    new List<string> { $"Vehicle Type with ID {id} not found" });
            }

            var fallbackIds = dto.FallbackVehicleTypeIds;

            if (fallbackIds.Contains(id))
            {
                return new Response<List<VehicleTypeFallbackDto>>(HttpStatusCode.BadRequest,
                    new List<string> { "A vehicle type cannot fall back to itself" });
            }

            if (fallbackIds.Distinct().Count() != fallbackIds.Count)
            {
                return new Response<List<VehicleTypeFallbackDto>>(HttpStatusCode.BadRequest,
                    new List<string> { "Fallback vehicle types must not repeat" });
            }

            var existingIds = await _uow.Context.VehicleTypes
                .Where(vt => fallbackIds.Contains(vt.Id))
                .Select(vt => vt.Id)
                .ToListAsync();

            var missing = fallbackIds.Except(existingIds).ToList();
            if (missing.Any())
            {
                return new Response<List<VehicleTypeFallbackDto>>(HttpStatusCode.BadRequest,
                    new List<string> { $"Vehicle Type(s) not found: {string.Join(", ", missing)}" });
            }

            var current = await _uow.Context.VehicleTypeFallbacks
                .Where(f => f.VehicleTypeId == id)
                .ToListAsync();
            _uow.Context.VehicleTypeFallbacks.RemoveRange(current);

            await _uow.Context.VehicleTypeFallbacks.AddRangeAsync(fallbackIds.Select((fallbackId, index) =>
                new Domain.Entities.VehicleTypeFallback
                {
                    VehicleTypeId = id,
                    FallbackVehicleTypeId = fallbackId,
                    Priority = index + 1
                }));

            await _uow.CompleteAsync();

            return new Response<List<VehicleTypeFallbackDto>>(HttpStatusCode.OK, "Vehicle Type fallbacks updated.",
                await LoadFallbacksAsync(id));
        }
        catch (Exception ex)
        {
            return new Response<List<VehicleTypeFallbackDto>>(HttpStatusCode.InternalServerError,
                new List<string> { ex.Message });
        }
    }

    private Task<List<VehicleTypeFallbackDto>> LoadFallbacksAsync(int id)
    {
        return _uow.Context.VehicleTypeFallbacks
            .Where(f => f.VehicleTypeId == id)
            .OrderBy(f => f.Priority)
            .Select(f => new VehicleTypeFallbackDto
            {
                VehicleTypeId = f.FallbackVehicleTypeId,
                Name = f.FallbackVehicleType.Name,
                Priority = f.Priority
            })
            .ToListAsync();
    }
}
