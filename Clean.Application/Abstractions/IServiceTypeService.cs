using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.ServiceType;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IServiceTypeService
{
    Task<Response<List<GetServiceTypeDto>>> GetAllAsync();
    Task<Response<GetServiceTypeDto>> CreateAsync(CreateServiceTypeDto dto);

    Task<Response<GetServiceTypeDto?>> UpdateAsync(UpdateServiceTypeDto dto);

    Task<Response<bool>> DeleteAsync(int id);
}