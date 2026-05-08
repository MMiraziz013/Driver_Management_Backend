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
}