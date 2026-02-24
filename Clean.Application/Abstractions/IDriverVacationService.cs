// Clean.Application/Abstractions/IDriverVacationService.cs

using Clean.Application.Dtos.Driver;
using Clean.Application.Dtos.DriverVacation;
using Clean.Application.Dtos.Responses;

namespace Clean.Application.Abstractions;

public interface IDriverVacationService
{
    Task<Response<GetDriverVacationDto>> AddVacationAsync(AddDriverVacationDto dto);
    Task<Response<GetDriverVacationDto?>> GetByIdAsync(int id);
    Task<Response<IEnumerable<GetDriverVacationDto>>> GetByDriverIdAsync(int driverId);
    Task<Response<IEnumerable<GetDriverVacationDto>>> GetAllAsync();
    Task<Response<IEnumerable<GetDriverVacationDto>>> GetActiveVacationsAsync();
    Task<Response<IEnumerable<GetDriverVacationDto>>> GetVacationsInRangeAsync(DateTime startDate, DateTime endDate);
    Task<Response<GetDriverVacationDto?>> UpdateVacationAsync(UpdateDriverVacationDto dto);
    Task<Response<bool>> DeleteVacationAsync(int id);
    Task<Response<bool>> IsDriverOnVacationAsync(int driverId, DateTime? date = null);
}