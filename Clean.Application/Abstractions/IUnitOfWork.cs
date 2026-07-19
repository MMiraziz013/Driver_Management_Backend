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
    
    ICachedLocationRepository CachedLocations { get; }
    
    // ===== NEW: Gas Management Repositories =====
    IGasPurchaseRepository GasPurchases { get; }
    IVehicleFuelAllocationRepository FuelAllocations { get; }
    IDriverPeriodStateRepository DriverPeriodStates { get; } 
    
    IBonusSettingsRepository BonusSettings { get; }
    
    IServiceTypeBonusConfigRepository ServiceTypeBonusConfigs { get; }
    
    IExchangeRateRepository ExchangeRates { get; }
    IAccountingReportRepository AccountingReports { get; }
    IAccountingTransactionRepository AccountingTransactions { get; }
    
    ICompanyCategoryRepository CompanyCategories { get; }
    ICompanyRepository Companies { get; }
    IVehicleUnavailablePeriodRepository VehicleUnavailablePeriods { get; }
    
    void RemoveRange<T>(IEnumerable<T> entities) where T : class;
    
    Task<int> CompleteAsync(); // Effectively SaveChangesAsync()
}
