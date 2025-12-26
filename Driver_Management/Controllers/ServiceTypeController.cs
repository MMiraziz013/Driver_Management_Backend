using Clean.Application.Abstractions;
using Clean.Application.Dtos.ServiceType;
using Clean.Application.Security.Permission;
using Microsoft.AspNetCore.Mvc;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/service-type")]
public class ServiceTypeController : ControllerBase
{
    private readonly IServiceTypeService _serviceTypeService;

    public ServiceTypeController(IServiceTypeService serviceTypeService)
    {
        _serviceTypeService = serviceTypeService;
    }

    [HttpGet]
    // [PermissionAuthorize(PermissionConstants.ServiceTypes.Manage)]
    public async Task<IActionResult> GetAll()
    {
        var response = await _serviceTypeService.GetAllAsync();
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost]
    // [PermissionAuthorize(PermissionConstants.ServiceTypes.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateServiceTypeDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _serviceTypeService.CreateAsync(dto);
        return StatusCode((int)response.StatusCode, response);
    }
}