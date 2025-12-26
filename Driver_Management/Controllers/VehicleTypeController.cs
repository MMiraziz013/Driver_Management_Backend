using Clean.Application.Abstractions;
using Clean.Application.Dtos.Filters;
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
    // [PermissionAuthorize(PermissionConstants.VehicleTypes.View)]
    public async Task<IActionResult> GetVehicleTypesAsync([FromQuery] PaginationFilter filter)
    {
        var response = await _vehicleTypeService.GetVehicleTypesAsync(filter);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("add")]
    // [PermissionAuthorize(PermissionConstants.VehicleTypes.Manage)]
    public async Task<IActionResult> AddVehicleTypeAsync(AddVehicleTypeDto dto)
    {
        var response = await _vehicleTypeService.AddVehicleTypeAsync(dto);
        return StatusCode(response.StatusCode, response);
    }
}