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
        var weeklyWorkLimit = 0;
        if (dto.EmploymentType == EmploymentType.FullTime ||  dto.EmploymentType == EmploymentType.Samarkand)
        {
            weeklyWorkLimit = 60;
        }
        else
        {
            weeklyWorkLimit = 30;
        }
        
        var toAdd = new Domain.Entities.Driver
        {
            Id = 0,
            FullName = dto.FullName,
            BirthDay = dto.BirthYear,
            Address = dto.Address,
            Category = dto.DriverCategories,
            EmploymentType = dto.EmploymentType,
            WeeklyWorkLimit = weeklyWorkLimit,
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

    public async Task<Response<GetDriverDto?>> GetDriverByIdAsync(int id)
    {
        var driver = await _driverRepository.GetDriverByIdAsync(id);
        if (driver is null)
        {
            return new Response<GetDriverDto?>(HttpStatusCode.NotFound, $"No driver with id: {id}" );
        }
        var dto = new GetDriverDto
        {
            Id = driver.Id,
            FullName = driver.FullName,
            Age = driver.Age,
            Address = driver.Address,
            EmploymentType = driver.EmploymentType,
            LicenseCategory = driver.Category,
            IsActive = driver.IsActive
        };

        return new Response<GetDriverDto?>(HttpStatusCode.OK, dto);
    }

    public async Task<PaginatedResponse<GetDriverDto>> GetDriverPaginatedAsync(PaginationFilter filter)
    {
        var (drivers, totalRecords) = await _driverRepository.GetDriversAsync(filter);

        var response = new PaginatedResponse<GetDriverDto>(drivers, filter.PageNumber, filter.PageSize, totalRecords);

        return response;
    }

    public async Task<Response<GetDriverDto?>> UpdateDriverAsync(UpdateDriverDto dto)
    {
        var toUpdate = await _driverRepository.GetDriverByIdAsync(dto.Id);
        if (toUpdate is null)
        {
            return new Response<GetDriverDto?>(HttpStatusCode.NotFound, $"No driver with id {dto.Id} to update");
        }
        
        if (string.IsNullOrEmpty(dto.FullName) == false)
        {
            toUpdate.FullName = dto.FullName;
        }

        if (string.IsNullOrEmpty(dto.Address) == false)
        {
            toUpdate.Address = dto.Address;
        }

        if (dto.BirthYear.HasValue)
        {
            toUpdate.BirthDay = dto.BirthYear.Value;
        }

        if (dto.DriverCategory.HasValue)
        {
            toUpdate.Category = dto.DriverCategory.Value;
        }

        if (dto.EmploymentType.HasValue)
        {
            toUpdate.EmploymentType = dto.EmploymentType.Value;
        }

        var updated = await _driverRepository.UpdateDriverAsync(toUpdate);
        if (updated is null)
        {
            return new Response<GetDriverDto?>(HttpStatusCode.BadRequest, "Error while updating the driver in the database");
        }

        var returned = new GetDriverDto
        {
            Id = updated.Id,
            FullName = updated.FullName,
            Age = updated.Age,
            Address = updated.Address,
            EmploymentType = updated.EmploymentType,
            LicenseCategory = updated.Category,
            IsActive = updated.IsActive
        };

        return new Response<GetDriverDto?>(HttpStatusCode.OK, returned);
    }

    public async Task<Response<bool>> DeleteDriverAsync(int id)
    {
        var exists = await _driverRepository.GetDriverByIdAsync(id);
        if (exists is null)
        {
            return new Response<bool>(HttpStatusCode.NotFound, $"No driver with id: {id}");
        }

        var isDeleted = await _driverRepository.DeleteDriverAsync(id);
        return new Response<bool>(HttpStatusCode.OK, isDeleted);
    }
    public async Task<Response<bool>> DeactivateDriverAsync(int id)
    {
        var exists = await _driverRepository.GetDriverByIdAsync(id);
        if (exists is null)
        {
            return new Response<bool>(HttpStatusCode.NotFound, $"No driver with id: {id}");
        }

        var isDeactivated = await _driverRepository.ChangeDriverStatusAsync(id);

        return new Response<bool>(HttpStatusCode.OK, isDeactivated);
    }
}