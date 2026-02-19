using Clean.Application.Abstractions;
using Clean.Application.Dtos.Vehicle;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehicleController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehicleController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet]
    [PermissionAuthorize(PermissionConstants.Vehicles.View)]
    public async Task<IActionResult> GetAllAsync()
    {
        var response = await _vehicleService.GetActiveAndInactiveAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    [PermissionAuthorize(PermissionConstants.Vehicles.Manage)]
    public async Task<IActionResult> CreateAsync(CreateVehicleDto dto)
    {
        var response = await _vehicleService.CreateVehicleAsync(dto);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPut]
    [PermissionAuthorize(PermissionConstants.Vehicles.Manage)]
    public async Task<IActionResult> UpdateAsync(UpdateVehicleDto dto)
    {
        var response = await _vehicleService.UpdateVehicleAsync(dto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    [PermissionAuthorize(PermissionConstants.Vehicles.Manage)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var response = await _vehicleService.DeleteVehicleAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id}")]
    [PermissionAuthorize(PermissionConstants.Vehicles.Manage)]
    public async Task<IActionResult> ChangeStatusAsync(int id)
    {
        var response = await _vehicleService.ChangeStatusAsync(id);
        return StatusCode(response.StatusCode, response);
    }
}