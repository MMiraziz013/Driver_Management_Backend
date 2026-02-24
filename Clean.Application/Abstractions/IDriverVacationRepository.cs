// Clean.Application/Abstractions/IDriverVacationRepository.cs

using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IDriverVacationRepository
{
    Task<DriverVacation?> GetByIdAsync(int id);
    Task<IEnumerable<DriverVacation>> GetByDriverIdAsync(int driverId);
    Task<IEnumerable<DriverVacation>> GetAllAsync();
    Task<IEnumerable<DriverVacation>> GetActiveVacationsAsync(DateTime date);
    Task<IEnumerable<DriverVacation>> GetVacationsInRangeAsync(DateTime startDate, DateTime endDate);
    Task<DriverVacation> AddAsync(DriverVacation vacation);
    Task<DriverVacation?> UpdateAsync(DriverVacation vacation);
    Task<bool> DeleteAsync(int id);
    Task<bool> IsDriverOnVacationAsync(int driverId, DateTime date);
    Task<bool> HasOverlappingVacationAsync(int driverId, DateTime startDate, DateTime endDate, int? excludeId = null);
}