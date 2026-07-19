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
                VehicleTypeName = v.VehicleType.Name,
                RequiredDriverCategory = v.RequiredDriverCategory.ToString(),
                IsActive = v.IsActive,
                FuelTankCapacity = v.FuelTankCapacity,
                FuelConsumptionPer100Km = v.FuelConsumptionPer100Km,
                FuelType = v.FuelType,
                InitialFuelLevel = v.InitialFuelLevel,
                CurrentMileage = v.CurrentMileage,
                PurchaseCostUsd = v.PurchaseCostUsd,
                PlanMonths = v.PlanMonths,
                ActiveFrom = v.ActiveFrom
            }).ToList();

            return new Response<List<GetVehicleDto>>(HttpStatusCode.OK, "Vehicles retrieved.", dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetVehicleDto>>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }

    public async Task<Response<List<GetVehicleDto>>> GetActiveAndInactiveAsync()
    {
        try
        {
            var vehicles = await _uow.Vehicles.GetActiveAndInactiveAsync();

            var returnedDtos = vehicles.Select(v => new GetVehicleDto
            {
                Id = v.Id,
                PlateNumber = v.PlateNumber,
                Model = v.Model,
                Color = v.Color,
                VehicleTypeName = v.VehicleType.Name,
                RequiredDriverCategory = v.RequiredDriverCategory.ToString(),
                IsActive = v.IsActive,
                FuelTankCapacity = v.FuelTankCapacity,
                FuelConsumptionPer100Km = v.FuelConsumptionPer100Km,
                FuelType = v.FuelType,
                InitialFuelLevel = v.InitialFuelLevel,
                CurrentMileage = v.CurrentMileage,
                PurchaseCostUsd = v.PurchaseCostUsd,
                PlanMonths = v.PlanMonths,
                ActiveFrom = v.ActiveFrom
            }).ToList();

            return new Response<List<GetVehicleDto>>(HttpStatusCode.OK, "Vehicles retrieved.", returnedDtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetVehicleDto>>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }

    public async Task<Response<Domain.Entities.Vehicle?>> GetVehicleByIdAsync(int id)
    {
        var vehicle = await _uow.Vehicles.GetByIdAsync(id);
        if (vehicle == null)
            return new Response<Domain.Entities.Vehicle?>(HttpStatusCode.NotFound, "Vehicle not found.");

        return new Response<Domain.Entities.Vehicle?>(HttpStatusCode.OK, vehicle);
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
                FuelTankCapacity = dto.FuelTankCapacity,
                FuelConsumptionPer100Km = dto.FuelConsumptionPer100Km,
                FuelType = dto.FuelType,
                InitialFuelLevel = dto.InitialFuelLevel,
                CurrentMileage = dto.CurrentMileage,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ActiveFrom = dto.ActiveFrom
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

    public async Task<Response<GetVehicleDto?>> UpdateVehicleAsync(UpdateVehicleDto dto)
    {
        var toUpdate = await _uow.Vehicles.GetByIdAsync(dto.Id);
        if (toUpdate is null)
        {
            return new Response<GetVehicleDto?>(HttpStatusCode.NotFound, $"No car with id {dto.Id}");
        }

        if (string.IsNullOrEmpty(dto.Color) == false)
        {
            toUpdate.Color = dto.Color;
        }

        if (string.IsNullOrEmpty(dto.Model) == false)
        {
            toUpdate.Model = dto.Model;
        }

        if (string.IsNullOrEmpty(dto.PlateNumber) == false)
        {
            toUpdate.PlateNumber = dto.PlateNumber;
        }

        if (dto.RequiredDriverCategory.HasValue)
        {
            toUpdate.RequiredDriverCategory = dto.RequiredDriverCategory.Value;
        }

        if (dto.VehicleTypeId.HasValue)
        {
            toUpdate.VehicleTypeId = dto.VehicleTypeId.Value;
        }

        if (dto.FuelTankCapacity > 0)
        {
            toUpdate.FuelTankCapacity = dto.FuelTankCapacity;
        }

        if (dto.FuelConsumptionPer100Km > 0)
        {
            toUpdate.FuelConsumptionPer100Km = dto.FuelConsumptionPer100Km;
        }

        if (string.IsNullOrEmpty(dto.FuelType) == false)
        {
            toUpdate.FuelType = dto.FuelType;
        }

        if (dto.InitialFuelLevel > 0)
        {
            toUpdate.InitialFuelLevel = dto.InitialFuelLevel;
        }

        if (dto.CurrentMileage > 0)
        {
            toUpdate.CurrentMileage = dto.CurrentMileage;
        }

        if (dto.PurchaseCostUsd > 0)
        {
            toUpdate.PurchaseCostUsd = dto.PurchaseCostUsd;
        }

        if (dto.PlanMonths > 0 && dto.PlanMonths.HasValue)
        {
            toUpdate.PlanMonths = dto.PlanMonths.Value;
        }

        if (dto.ActiveFrom.HasValue)
        {
            toUpdate.ActiveFrom = dto.ActiveFrom;
        }
        
        var updated = await _uow.Vehicles.Update(toUpdate);
        if (updated is null)
        {
            return new Response<GetVehicleDto?>(HttpStatusCode.BadRequest,
                "Error while updating the vehicle in the database");
        }

        var returned = new GetVehicleDto
        {
            Id = updated.Id,
            PlateNumber = updated.PlateNumber,
            Model = updated.Model,
            Color = updated.Color,
            VehicleTypeName = updated.VehicleType.Name,
            RequiredDriverCategory = updated.RequiredDriverCategory.ToString(),
            IsActive = updated.IsActive,
            FuelTankCapacity = updated.FuelTankCapacity,
            FuelConsumptionPer100Km = updated.FuelConsumptionPer100Km,
            FuelType = updated.FuelType,
            InitialFuelLevel = updated.InitialFuelLevel,
            CurrentMileage = updated.CurrentMileage,
            PurchaseCostUsd = updated.PurchaseCostUsd,
            PlanMonths = updated.PlanMonths,
            ActiveFrom = updated.ActiveFrom,
        };

        return new Response<GetVehicleDto?>(HttpStatusCode.OK, returned);
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
    
    public async Task<Response<bool>> ChangeStatusAsync(int id)
    {
        try
        {
            var isDeactivated = await _uow.Vehicles.ChangeStatus(id);
            if (isDeactivated == false)
            {
                return new Response<bool>(HttpStatusCode.BadRequest,
                    "Failed to deactivate the vehicle, or vehicle not found");
            }

            return new Response<bool>(HttpStatusCode.OK, isDeactivated);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
}