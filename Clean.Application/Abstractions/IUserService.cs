using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.User;

namespace Clean.Application.Abstractions;

public interface IUserService
{
    Task<Response<string>> RegisterUserAsync(RegisterUserDto register);
    
    Task<Response<object>> LoginUserAsync(LoginDto login);

    Task<PaginatedResponse<GetUserDto>> GetUsersAsync(PaginationFilter filter);

    Task<Response<GetUserDto?>> GetUserByIdAsync(int id);
}