using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Driver;
using Clean.Application.Dtos.Filters;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/drivers")]
public class DriverController : Controller
{
    private readonly IDriverService _driverService;

    public DriverController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    [HttpPost("add")]
    [PermissionAuthorize(PermissionConstants.Drivers.ManageAll)]
    public async Task<IActionResult> AddDriverAsync(AddDriverDto dto)
    {
        var response = await _driverService.AddDriverAsync(dto);
        return StatusCode(response.StatusCode, response);
    }


    [HttpGet]
    [PermissionAuthorize(PermissionConstants.Drivers.View)]
    public async Task<IActionResult> GetDriversAsync([FromQuery] PaginationFilter filter)
    {
        var response = await _driverService.GetDriverPaginatedAsync(filter);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut]
    [PermissionAuthorize(PermissionConstants.Drivers.ManageAll)]
    public async Task<IActionResult> EditDriverAsync(UpdateDriverDto dto)
    {
        var response = await _driverService.UpdateDriverAsync(dto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    [PermissionAuthorize(PermissionConstants.Drivers.ManageAll)]
    public async Task<IActionResult> DeleteDriverAsync(int id)
    {
        var response = await _driverService.DeleteDriverAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id}")]
    [PermissionAuthorize(PermissionConstants.Drivers.Manage)]
    public async Task<IActionResult> DeactivateDriverAsync(int id)
    {
        var response = await _driverService.DeactivateDriverAsync(id);
        return StatusCode(response.StatusCode, response);
    }
}