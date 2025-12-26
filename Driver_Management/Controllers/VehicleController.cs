using Clean.Application.Abstractions;
using Clean.Application.Dtos.Vehicle;
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
    public async Task<IActionResult> GetAllAsync() => 
        Ok(await _vehicleService.GetAllVehiclesAsync());

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateVehicleDto dto)
    {
        var response = await _vehicleService.CreateVehicleAsync(dto);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var response = await _vehicleService.DeleteVehicleAsync(id);
        return StatusCode(response.StatusCode, response);
    }
}