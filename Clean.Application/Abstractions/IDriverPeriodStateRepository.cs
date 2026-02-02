using Clean.Domain.Entities;

namespace Clean.Application.Abstractions;

public interface IDriverPeriodStateRepository
{
    /// <summary>
    /// Get a specific driver's seat for a specific period
    /// </summary>
    Task<DriverPeriodState?> GetByDriverAndPeriodAsync(int driverId, int periodId);
    
    /// <summary>
    /// Get all driver states for a period
    /// </summary>
    Task<List<DriverPeriodState>> GetByPeriodIdAsync(int periodId);
    
    /// <summary>
    /// Get the most recent state for a driver (from a latest finalized period)
    /// </summary>
    Task<DriverPeriodState?> GetLatestForDriverAsync(int driverId);
    
    /// <summary>
    /// Get states for multiple drivers from a specific period
    /// </summary>
    Task<List<DriverPeriodState>> GetByDriverIdsAndPeriodAsync(IEnumerable<int> driverIds, int periodId);
    
    /// <summary>
    /// Check if states exist for a period (i.e., was it finalized?)
    /// </summary>
    Task<bool> ExistsForPeriodAsync(int periodId);
    
    /// <summary>
    /// Add a single driver period state
    /// </summary>
    Task AddAsync(DriverPeriodState state);
    
    /// <summary>
    /// Add multiple driver period states
    /// </summary>
    Task AddRangeAsync(IEnumerable<DriverPeriodState> states);
    
    /// <summary>
    /// Delete all states for a period (used when reverting finalization)
    /// </summary>
    Task DeleteByPeriodIdAsync(int periodId);
    
    /// <summary>
    /// Get all states for a driver (history across periods)
    /// </summary>
    Task<List<DriverPeriodState>> GetDriverHistoryAsync(int driverId);

}