using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.Vehicle;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IVehicleService
{
    Task<Response<List<GetVehicleDto>>> GetAllVehiclesAsync();
    Task<Response<Vehicle>> GetVehicleByIdAsync(int id);
    Task<Response<Vehicle>> CreateVehicleAsync(CreateVehicleDto dto);

    Task<Response<bool>> DeleteVehicleAsync(int id);
}

