using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.VehicleType;

namespace Clean.Application.Abstractions;

public interface IVehicleTypeService
{
    Task<PaginatedResponse<GetVehicleTypeDto>> GetVehicleTypesAsync(PaginationFilter filter);
    Task<Response<GetVehicleTypeDto>> AddVehicleTypeAsync(AddVehicleTypeDto dto); // Added
    Task<Response<GetVehicleTypeDto>> UpdateVehicleTypeAsync(int id, UpdateVehicleTypeDto dto);
}