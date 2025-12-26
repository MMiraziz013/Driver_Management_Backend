using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.User;
using Clean.Domain.Entities;
using Clean.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClassLibrary1.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DataContext _context;
    private readonly UserManager<User> _userManager;

    public UserRepository(DataContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    public async Task<(List<GetUserDto> Users, int TotalRecords)> GetUsersAsync(PaginationFilter filter)
    {
        var query = _context.Users
            .AsQueryable();
        
        var totalRecords = await query.CountAsync();

        query = query
            .OrderBy(e => e.Id)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize);

        var list = await query.Select(u => new GetUserDto
        {
            Id = u.Id,
            Username = u.UserName,
            Email = u.Email,
            Phone = u.PhoneNumber,
            FirstName = u.FirstName,
            LastName = u.LastName,
            IsActive = u.IsActive,
            Role = u.Role
        }).ToListAsync();
        
        return (list, totalRecords);
    }

    public Task<User?> GetUserByIdAsync(int id)
    {
        throw new NotImplementedException();
    }
}