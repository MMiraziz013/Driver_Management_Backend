using Clean.Application.Dtos.Driver;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IDriverService
{
    public Task<Response<GetDriverDto>> AddDriverAsync(AddDriverDto dto);
    public Task<Response<GetDriverDto?>> GetDriverByIdAsync(int id);
    public Task<PaginatedResponse<GetDriverDto>> GetDriverPaginatedAsync(PaginationFilter filter);
    public Task<Response<GetDriverDto?>> UpdateDriverAsync(UpdateDriverDto dto);
    public Task<Response<bool>> DeactivateDriverAsync(int id);

    Task<Response<bool>> DeleteDriverAsync(int id);
}