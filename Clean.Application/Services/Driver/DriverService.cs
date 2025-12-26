using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Driver;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Enums;

namespace Clean.Application.Services.Driver;

public class DriverService : IDriverService
{
    private readonly IDriverRepository _driverRepository;

    public DriverService(IDriverRepository driverRepository)
    {
        _driverRepository = driverRepository;
    }
    
    public async Task<Response<GetDriverDto>> AddDriverAsync(AddDriverDto dto)
    {
        var toAdd = new Domain.Entities.Driver
        {
            Id = 0,
            FullName = dto.FullName,
            BirthDay = dto.BirthYear,
            Address = dto.Address,
            Category = dto.DriverCategories,
            EmploymentType = dto.EmploymentType,
            WeeklyWorkLimit = dto.EmploymentType == EmploymentType.FullTime ? 60 : 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var driver = await _driverRepository.AddDriverAsync(toAdd);
        if (driver is null)
        {
            return new Response<GetDriverDto>(HttpStatusCode.BadRequest, message: "Error while adding the driver");
        }

        var added = new GetDriverDto
        {
            Id = driver.Id,
            FullName = driver.FullName,
            Age = driver.Age,
            Address = driver.Address,
            EmploymentType = driver.EmploymentType,
            LicenseCategory = driver.Category,
            IsActive = driver.IsActive
        };

        return new Response<GetDriverDto>(HttpStatusCode.OK, added);
    }

    public Response<GetDriverDto?> GetDriverByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<PaginatedResponse<GetDriverDto>> GetDriverPaginatedAsync(PaginationFilter filter)
    {
        var (drivers, totalRecords) = await _driverRepository.GetDriversAsync(filter);

        var response = new PaginatedResponse<GetDriverDto>(drivers, filter.PageNumber, filter.PageSize, totalRecords);

        return response;
    }

    public Response<GetDriverDto?> UpdateDriverAsync(UpdateDriverDto dto)
    {
        throw new NotImplementedException();
    }

    public Response<bool> DeactivateDriverAsync(int id)
    {
        throw new NotImplementedException();
    }
}