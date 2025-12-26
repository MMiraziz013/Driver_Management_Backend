using ClassLibrary1.Data;
using ClassLibrary1.Data.Repositories;
using Clean.Application.Abstractions;

public class UnitOfWork : IUnitOfWork
{
    private readonly DataContext _context;

    public IDataContext Context => _context;

    public IDriverRepository Drivers { get; }
    public ITripRepository Trips { get; }
    public IVehicleRepository Vehicles { get; }
    public IReportPeriodRepository ReportPeriods { get; }
    public IVehicleTypeRepository VehicleTypes { get; }
    public IServiceTypeRepository ServiceTypes { get; }

    public UnitOfWork(DataContext context) // Removed the extra parameter
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        
        // Initialize all repositories
        Drivers = new DriverRepository(_context);
        Trips = new TripRepository(_context);
        Vehicles = new VehicleRepository(_context);
        ReportPeriods = new ReportPeriodRepository(_context);
        VehicleTypes = new VehicleTypeRepository(_context);
        ServiceTypes = new ServiceTypeRepository(_context);
    }

    public void RemoveRange<T>(IEnumerable<T> entities) where T : class
    {
        _context.Set<T>().RemoveRange(entities);
    }
    
    public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}