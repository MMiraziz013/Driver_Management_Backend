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
}