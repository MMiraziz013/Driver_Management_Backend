using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Vehicle;
using Clean.Application.Dtos.VehicleType;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/vehicle-types")]
public class VehicleTypeController : Controller
{
    private readonly IVehicleTypeService _vehicleTypeService;

    public VehicleTypeController(IVehicleTypeService vehicleTypeService)
    {
        _vehicleTypeService = vehicleTypeService;
    }

    [HttpGet]
    [PermissionAuthorize(PermissionConstants.VehicleTypes.View)]
    public async Task<IActionResult> GetVehicleTypesAsync([FromQuery] PaginationFilter filter)
    {
        var response = await _vehicleTypeService.GetVehicleTypesAsync(filter);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("add")]
    [PermissionAuthorize(PermissionConstants.VehicleTypes.Manage)]
    public async Task<IActionResult> AddVehicleTypeAsync(AddVehicleTypeDto dto)
    {
        var response = await _vehicleTypeService.AddVehicleTypeAsync(dto);
        return StatusCode(response.StatusCode, response);
    }
    

    [HttpPut("{id:int}")]
    [PermissionAuthorize(PermissionConstants.VehicleTypes.Manage)]
    public async Task<IActionResult> UpdateVehicleType(int id, [FromBody] UpdateVehicleTypeDto dto)
    {
        var result = await _vehicleTypeService.UpdateVehicleTypeAsync(id, dto);
        return StatusCode((int)result.StatusCode, result);
    }

    [HttpGet("{id:int}/fallbacks")]
    [PermissionAuthorize(PermissionConstants.VehicleTypes.View)]
    public async Task<IActionResult> GetFallbacks(int id)
    {
        var result = await _vehicleTypeService.GetFallbacksAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:int}/fallbacks")]
    [PermissionAuthorize(PermissionConstants.VehicleTypes.Manage)]
    public async Task<IActionResult> UpdateFallbacks(int id, [FromBody] UpdateVehicleTypeFallbacksDto dto)
    {
        var result = await _vehicleTypeService.UpdateFallbacksAsync(id, dto);
        return StatusCode(result.StatusCode, result);
    }

    //TODO: Implement these features below

    [HttpDelete]
    [PermissionAuthorize(PermissionConstants.VehicleTypes.Manage)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }
}