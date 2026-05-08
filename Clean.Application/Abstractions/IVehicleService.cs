using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.Vehicle;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IVehicleService
{
    Task<Response<List<GetVehicleDto>>> GetAllVehiclesAsync();
    
    Task<Response<List<GetVehicleDto>>> GetActiveAndInactiveAsync();
    Task<Response<Vehicle?>> GetVehicleByIdAsync(int id);
    Task<Response<Vehicle>> CreateVehicleAsync(CreateVehicleDto dto);

    Task<Response<GetVehicleDto?>> UpdateVehicleAsync(UpdateVehicleDto dto);
    Task<Response<bool>> DeleteVehicleAsync(int id);

    Task<Response<bool>> ChangeStatusAsync(int id);
}

