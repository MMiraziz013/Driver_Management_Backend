using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.User;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IUserRepository
{

    public Task<(List<GetUserDto> Users, int TotalRecords)> GetUsersAsync(PaginationFilter filter);
    public Task<User?> GetUserByIdAsync(int id);
}