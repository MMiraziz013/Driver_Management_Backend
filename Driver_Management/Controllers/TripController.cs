using Clean.Application.Abstractions;
using Clean.Application.Dtos.Trip;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/trips")]
[Authorize]  // Just require authentication, not specific permission
public class TripController : Controller
{
    private readonly ITripService _tripService;

    public TripController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTripsByPeriod([FromQuery] int periodId)
    {
        var response = await _tripService.GetTripsByPeriodAsync(periodId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTripById(int id)
    {
        var response = await _tripService.GetTripByIdAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTrip(int id, [FromBody] UpdateTripDto dto)
    {
        dto.Id = id;
        var response = await _tripService.UpdateTripAsync(dto);
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("{id}/recalculate-distance")]
    public async Task<IActionResult> RecalculateDistance(int id)
    {
        var response = await _tripService.RecalculateTripDistanceAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTrip(int id)
    {
        var response = await _tripService.DeleteTripAsync(id);
        return StatusCode(response.StatusCode, response);
    }
}