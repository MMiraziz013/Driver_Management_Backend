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
    public async Task<IActionResult> GetAllAsync()
    {
        var response = await _vehicleService.GetAllVehiclesAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateVehicleDto dto)
    {
        var response = await _vehicleService.CreateVehicleAsync(dto);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateAsync(UpdateVehicleDto dto)
    {
        var response = await _vehicleService.UpdateVehicleAsync(dto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var response = await _vehicleService.DeleteVehicleAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> ChangeStatusAsync(int id)
    {
        var response = await _vehicleService.ChangeStatusAsync(id);
        return StatusCode(response.StatusCode, response);
    }
}