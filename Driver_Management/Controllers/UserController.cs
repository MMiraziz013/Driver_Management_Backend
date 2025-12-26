using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.User;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/")]
public class UserController : Controller
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpPost("user/register")]
    public async Task<IActionResult> RegisterAsync(RegisterUserDto dto)
    {
        var response = await _userService.RegisterUserAsync(dto);
        return StatusCode(response.StatusCode, response);
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginDto dto)
    {
        var response = await _userService.LoginUserAsync(dto);
        return StatusCode(response.StatusCode, response);    
    }

    [PermissionAuthorize(PermissionConstants.Users.ManageAll)]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsersAsync([FromQuery] PaginationFilter filter)
    {
        var response = await _userService.GetUsersAsync(filter);
        return StatusCode(response.StatusCode, response);
    }
    
    
}