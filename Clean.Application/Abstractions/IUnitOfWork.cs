namespace Clean.Application.Abstractions;

public interface IUnitOfWork : IDisposable
{
    IDataContext Context { get; }
    
    IDriverRepository Drivers { get; }
    ITripRepository Trips { get; }
    IVehicleRepository Vehicles { get; }
    IReportPeriodRepository ReportPeriods { get; }
    IVehicleTypeRepository VehicleTypes { get; }
    IServiceTypeRepository ServiceTypes { get; }
    
    void RemoveRange<T>(IEnumerable<T> entities) where T : class;
    
    Task<int> CompleteAsync(); // Effectively SaveChangesAsync()
}
