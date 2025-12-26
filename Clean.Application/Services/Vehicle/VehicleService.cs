using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.Vehicle;

namespace Clean.Application.Services.Vehicle;

public class VehicleService : IVehicleService
{
    private readonly IUnitOfWork _uow;

    public VehicleService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<List<GetVehicleDto>>> GetAllVehiclesAsync()
    {
        try
        {
            var vehicles = await _uow.Vehicles.GetAllAsync();
        
            var dtos = vehicles.Select(v => new GetVehicleDto
            {
                Id = v.Id,
                PlateNumber = v.PlateNumber,
                Model = v.Model,
                Color = v.Color,
                VehicleTypeName = v.VehicleType?.Name ?? "N/A",
                RequiredDriverCategory = v.RequiredDriverCategory.ToString()
            }).ToList();

            return new Response<List<GetVehicleDto>>(HttpStatusCode.OK, "Vehicles retrieved.", dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetVehicleDto>>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }
    public async Task<Response<Domain.Entities.Vehicle>> GetVehicleByIdAsync(int id)
    {
        var vehicle = await _uow.Vehicles.GetByIdAsync(id);
        if (vehicle == null)
            return new Response<Domain.Entities.Vehicle>(HttpStatusCode.NotFound, "Vehicle not found.");

        return new Response<Domain.Entities.Vehicle>(HttpStatusCode.OK, vehicle);
    }

    public async Task<Response<Domain.Entities.Vehicle>> CreateVehicleAsync(CreateVehicleDto dto)
    {
        try
        {
            var vehicle = new Domain.Entities.Vehicle
            {
                PlateNumber = dto.PlateNumber,
                Model = dto.Model,
                Color = dto.Color,
                VehicleTypeId = dto.VehicleTypeId,
                RequiredDriverCategory = dto.RequiredDriverCategory,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _uow.Vehicles.AddAsync(vehicle);
            await _uow.CompleteAsync();

            return new Response<Domain.Entities.Vehicle>(HttpStatusCode.Created, "Vehicle created.", vehicle);
        }
        catch (Exception ex)
        {
            return new Response<Domain.Entities.Vehicle>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }

    public async Task<Response<bool>> DeleteVehicleAsync(int id)
    {
        var isDeleted = await _uow.Vehicles.Delete(id);
        if (isDeleted == false)
        {
            return new Response<bool>(HttpStatusCode.BadRequest, "Failed to delete the vehicle, or vehicle not found");
        }
        return new Response<bool>(HttpStatusCode.OK, isDeleted);
    }

    public async Task<Response<List<Domain.Entities.Vehicle>>> CreateBulkAsync(List<CreateVehicleDto> dtos)
    {
        try
        {
            var vehicles = dtos.Select(dto => new Domain.Entities.Vehicle
            {
                PlateNumber = dto.PlateNumber,
                Model = dto.Model,
                Color = dto.Color,
                VehicleTypeId = dto.VehicleTypeId,
                RequiredDriverCategory = dto.RequiredDriverCategory,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            foreach (var v in vehicles) await _uow.Vehicles.AddAsync(v);
            await _uow.CompleteAsync();

            return new Response<List<Domain.Entities.Vehicle>>(HttpStatusCode.Created, $"{vehicles.Count} vehicles added.", vehicles);
        }
        catch (Exception ex)
        {
            return new Response<List<Domain.Entities.Vehicle>>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }}