using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.User;
using Clean.Application.Services.Enum;
using Clean.Application.Services.JWT;
using Clean.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Clean.Application.Services.User;

public class UserService : IUserService
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IJwtTokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;

    public UserService(
        UserManager<Domain.Entities.User> userManager,
        IJwtTokenService tokenService,
        IConfiguration configuration,
        IUserRepository userRepository)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _configuration = configuration;
        _userRepository = userRepository;
    }
    
    public async Task<Response<string>> RegisterUserAsync(RegisterUserDto register)
    {
        var existing = await _userManager.FindByEmailAsync(register.Email);
        if (existing != null)
        {
            return new Response<string>("A user with this email already exists.");
        }

        var user = new Domain.Entities.User
        {
            UserName = register.Email,
            Email = register.Email,
            FirstName = register.FirstName,
            LastName = register.LastName,
            Role = UserRole.Employee,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, register.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return new Response<string>($"User registration failed: {errors}");
        }

        await _userManager.AddToRoleAsync(user, user.Role.GetDisplayName());

        return new Response<string>("User registered successfully.");
    }


    public async Task<Response<object>> LoginUserAsync(LoginDto login)
    {
        var user = await _userManager.FindByNameAsync(login.Username);
        if (user is null)
            return new Response<object>(HttpStatusCode.BadRequest, "No user with this username.");

        if (!await _userManager.CheckPasswordAsync(user, login.Password))
            return new Response<object>(HttpStatusCode.BadRequest, "Incorrect password.");

        var jwtToken = await _tokenService.GenerateJwtToken(user);
        return new Response<object>(HttpStatusCode.OK, "Login Successful", new
        {
            Token = jwtToken,
            ExpiresAt = DateTime.Now.AddMinutes(double.Parse(_configuration["JWT:AccessTokenMinutes"]!)).ToString("g")
        });

    }

    public async Task<PaginatedResponse<GetUserDto>> GetUsersAsync(PaginationFilter filter)
    {
        var users = await _userRepository.GetUsersAsync(filter);


        return new PaginatedResponse<GetUserDto>(users.Users, filter.PageNumber, filter.PageSize, users.TotalRecords);
    }

    public async Task<Response<GetUserDto?>> GetUserByIdAsync(int id)
    {
        var entity = await _userRepository.GetUserByIdAsync(id);
        if (entity is null)
        {
            throw new ArgumentNullException(nameof(id), "No such user in the system!");
        }
        var user = new GetUserDto
        {
            Id = entity.Id,
            Username = entity.UserName,
            Email = entity.Email,
            Phone = entity.PhoneNumber,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            IsActive = entity.IsActive,
            Role = entity.Role
        };


        return new Response<GetUserDto?>(HttpStatusCode.OK, user);
    }
}