using System.Net;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Responses;
using Clean.Application.Dtos.ServiceType;

namespace Clean.Application.Services.ServiceType;

public class ServiceTypeService : IServiceTypeService
{
    private readonly IUnitOfWork _uow;

    public ServiceTypeService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Response<List<GetServiceTypeDto>>> GetAllAsync()
    {
        try
        {
            var serviceTypes = await _uow.ServiceTypes.GetAllAsync();
            
            var dtos = serviceTypes.Select(st => new GetServiceTypeDto 
            { 
                Id = st.Id, 
                Name = st.Name, 
                Description = st.Description 
            }).ToList();
            
            return new Response<List<GetServiceTypeDto>>(HttpStatusCode.OK, "Service types retrieved successfully.", dtos);
        }
        catch (Exception ex)
        {
            return new Response<List<GetServiceTypeDto>>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }

    public async Task<Response<GetServiceTypeDto>> CreateAsync(CreateServiceTypeDto dto)
    {
        try
        {
            var serviceType = new Domain.Entities.ServiceType 
            { 
                Name = dto.Name, 
                Description = dto.Description 
            };

            await _uow.ServiceTypes.AddAsync(serviceType);
            await _uow.CompleteAsync();

            var result = new GetServiceTypeDto 
            { 
                Id = serviceType.Id, 
                Name = serviceType.Name, 
                Description = serviceType.Description 
            };
            
            return new Response<GetServiceTypeDto>(HttpStatusCode.Created, "Service type created successfully.", result);
        }
        catch (Exception ex)
        {
            return new Response<GetServiceTypeDto>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }

    public async Task<Response<GetServiceTypeDto?>> UpdateAsync(UpdateServiceTypeDto dto)
    {
        try
        {
            var exists =  await _uow.ServiceTypes.GetByIdAsync(dto.Id);
            if (exists is null)
            {
                throw new ArgumentException("No such service type to update",$"{dto.Name}");
            }

            if (string.IsNullOrEmpty(dto.Name) == false)
            {
                exists.Name = dto.Name;
            }

            if (string.IsNullOrEmpty(dto.Description) == false)
            {
                exists.Description = dto.Description;
            }

            var isUpdated = await _uow.ServiceTypes.UpdateAsync(exists);
            if (isUpdated is null)
            {
                return new Response<GetServiceTypeDto?>(HttpStatusCode.InternalServerError,
                "Error while updating the service type");
            }

            var updatedDto = new GetServiceTypeDto
            {
                Id = isUpdated.Id,
                Name = isUpdated.Name,
                Description = isUpdated.Description
            };

            return new Response<GetServiceTypeDto?>(HttpStatusCode.OK, updatedDto);
        }
        catch (Exception ex)
        {
            return new Response<GetServiceTypeDto?>(HttpStatusCode.InternalServerError, new List<string> { ex.Message });
        }
    }

    public async Task<Response<bool>> DeleteAsync(int id)
    {
        var result = await _uow.ServiceTypes.DeleteAsync(id);
        return new Response<bool>(HttpStatusCode.OK, result);
    }
}