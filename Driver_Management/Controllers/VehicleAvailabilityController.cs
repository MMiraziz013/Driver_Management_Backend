using Clean.Application.Abstractions;
using Clean.Application.Dtos.Vehicle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehicleAvailabilityController : ControllerBase
{
    private readonly IVehicleAvailabilityService _service;

    public VehicleAvailabilityController(IVehicleAvailabilityService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get all unavailable periods for a specific vehicle
    /// </summary>
    [HttpGet("{vehicleId:int}/unavailable-periods")]
    public async Task<IActionResult> GetByVehicleId(int vehicleId)
    {
        var periods = await _service.GetByVehicleIdAsync(vehicleId);
        return Ok(new { data = periods });
    }

    /// <summary>
    /// Get all unavailable periods across all vehicles
    /// </summary>
    [HttpGet("unavailable-periods")]
    public async Task<IActionResult> GetAll()
    {
        var periods = await _service.GetAllAsync();
        return Ok(new { data = periods });
    }

    /// <summary>
    /// Create a new unavailable period for a vehicle
    /// </summary>
    [HttpPost("unavailable-periods")]
    public async Task<IActionResult> Create([FromBody] CreateVehicleUnavailablePeriodDto dto)
    {
        if (dto.EndDate < dto.StartDate)
        {
            return BadRequest(new { message = "End date must be on or after start date" });
        }

        var result = await _service.CreateAsync(dto);
        if (result == null)
        {
            return BadRequest(new { message = "Failed to create period. It may overlap with an existing period." });
        }

        return Ok(new { data = result, message = "Unavailable period created successfully" });
    }

    /// <summary>
    /// Update an existing unavailable period
    /// </summary>
    [HttpPut("unavailable-periods/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleUnavailablePeriodDto dto)
    {
        if (dto.Id != id)
        {
            dto.Id = id;
        }

        if (dto.EndDate < dto.StartDate)
        {
            return BadRequest(new { message = "End date must be on or after start date" });
        }

        var result = await _service.UpdateAsync(dto);
        if (result == null)
        {
            return BadRequest(new { message = "Failed to update period. It may overlap with another period." });
        }

        return Ok(new { data = result, message = "Unavailable period updated successfully" });
    }

    /// <summary>
    /// Delete an unavailable period
    /// </summary>
    [HttpDelete("unavailable-periods/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success)
        {
            return NotFound(new { message = "Period not found" });
        }

        return Ok(new { message = "Unavailable period deleted successfully" });
    }

    /// <summary>
    /// Check if a vehicle is available on a specific date
    /// </summary>
    [HttpGet("{vehicleId}/available")]
    public async Task<IActionResult> CheckAvailability(int vehicleId, [FromQuery] DateTime date)
    {
        var isAvailable = await _service.IsVehicleAvailableOnDateAsync(vehicleId, date);
        return Ok(new { vehicleId, date = date.Date, isAvailable });
    }
}