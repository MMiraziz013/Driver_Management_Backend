// Driver_Management/Controllers/DriverVacationController.cs

using Clean.Application.Abstractions;
using Clean.Application.Dtos.Driver;
using Clean.Application.Dtos.DriverVacation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/driver-vacations")]
[Authorize]
public class DriverVacationController : ControllerBase
{
    private readonly IDriverVacationService _vacationService;

    public DriverVacationController(IDriverVacationService vacationService)
    {
        _vacationService = vacationService;
    }

    /// <summary>
    /// Get all vacations
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var response = await _vacationService.GetAllAsync();
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Get currently active vacations
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var response = await _vacationService.GetActiveVacationsAsync();
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Get vacations within a date range
    /// </summary>
    [HttpGet("range")]
    public async Task<IActionResult> GetInRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var response = await _vacationService.GetVacationsInRangeAsync(startDate, endDate);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Get vacation by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _vacationService.GetByIdAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Get all vacations for a specific driver
    /// </summary>
    [HttpGet("driver/{driverId}")]
    public async Task<IActionResult> GetByDriver(int driverId)
    {
        var response = await _vacationService.GetByDriverIdAsync(driverId);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Check if a driver is currently on vacation
    /// </summary>
    [HttpGet("driver/{driverId}/status")]
    public async Task<IActionResult> CheckDriverStatus(int driverId, [FromQuery] DateTime? date = null)
    {
        var response = await _vacationService.IsDriverOnVacationAsync(driverId, date);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Create a new vacation
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddDriverVacationDto dto)
    {
        var response = await _vacationService.AddVacationAsync(dto);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Update a vacation
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateDriverVacationDto dto)
    {
        var response = await _vacationService.UpdateVacationAsync(dto);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>
    /// Delete a vacation
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _vacationService.DeleteVacationAsync(id);
        return StatusCode((int)response.StatusCode, response);
    }
}