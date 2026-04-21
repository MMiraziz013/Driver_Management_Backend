using ClassLibrary1.Data.Repositories;
using Clean.Application.Abstractions;

namespace ClassLibrary1.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly DataContext _context;

    // Existing repositories
    private IDriverRepository? _drivers;
    private ITripRepository? _trips;
    private IVehicleRepository? _vehicles;
    private IReportPeriodRepository? _reportPeriods;
    private IVehicleTypeRepository? _vehicleTypes;
    private IServiceTypeRepository? _serviceTypes;
    private ICachedLocationRepository? _cachedLocations;
    
    // NEW: Gas Management repositories
    private IGasPurchaseRepository? _gasPurchases;
    private IVehicleFuelAllocationRepository? _fuelAllocations;

    private IDriverPeriodStateRepository? _driverPeriodStates;

    public UnitOfWork(DataContext context)
    {
        _context = context;
    }

    public IDataContext Context => _context;

    // Existing repository properties
    public IDriverRepository Drivers => _drivers ??= new DriverRepository(_context);
    public ITripRepository Trips => _trips ??= new TripRepository(_context);
    public IVehicleRepository Vehicles => _vehicles ??= new VehicleRepository(_context);
    public IReportPeriodRepository ReportPeriods => _reportPeriods ??= new ReportPeriodRepository(_context);
    public IVehicleTypeRepository VehicleTypes => _vehicleTypes ??= new VehicleTypeRepository(_context);
    public IServiceTypeRepository ServiceTypes => _serviceTypes ??= new ServiceTypeRepository(_context);
    public ICachedLocationRepository CachedLocations => _cachedLocations ??= new CachedLocationRepository(_context);

    // NEW: Gas Management repository properties
    public IGasPurchaseRepository GasPurchases => _gasPurchases ??= new GasPurchaseRepository(_context);
    public IVehicleFuelAllocationRepository FuelAllocations => _fuelAllocations ??= new VehicleFuelAllocationRepository(_context);
    public IDriverPeriodStateRepository DriverPeriodStates => _driverPeriodStates ??= new DriverPeriodStateRepository(_context);

    public void RemoveRange<T>(IEnumerable<T> entities) where T : class
    {
        _context.Set<T>().RemoveRange(entities);
    }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}