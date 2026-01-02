using Clean.Application.Dtos.Driver;
using Clean.Application.Dtos.Filters;
using Clean.Application.Dtos.Responses;
using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IDriverRepository
{
    Task<Driver?> AddDriverAsync(Driver driver);
    Task<Driver?> GetDriverByIdAsync(int id);
    Task<(List<GetDriverDto> drivers, int totalRecords)> GetDriversAsync(PaginationFilter filter);
    
    Task<List<Driver>> GetActiveDriversWithDetailsAsync();

    Task<Driver?> UpdateDriverAsync(Driver driver);

    Task<bool> DeleteDriverAsync(int id);
}