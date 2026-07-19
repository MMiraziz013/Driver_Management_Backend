using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Clean.Application.Abstractions;

public interface IDataContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<DriverAssignment> DriverAssignments { get; set; }
    public DbSet<DriverOffDay> DriverOffDays { get; set; }
    public DbSet<DriverVacation> DriverVacations { get; set; }
    public DbSet<Filter> Filters { get; set; }
    public DbSet<ReportPeriod> ReportPeriods { get; set; }
    public DbSet<ServiceType> ServiceTypes { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<VehicleType> VehicleTypes { get; set; }
    
    public DbSet<CachedLocation> CachedLocations { get; set; }
    
    public DbSet<GasPurchase> GasPurchases { get; set; }
    
    public DbSet<VehicleFuelAllocation> VehicleFuelAllocations { get; set; }

    public DbSet<DriverPeriodState> DriverPeriodStates { get; set; }
    
    public DbSet<BonusSettings>  BonusSettings { get; set; }

    public DbSet<ServiceTypeBonusConfig> ServiceTypeBonusConfigs { get; set; }

    public DbSet<ExchangeRate> ExchangeRates { get; set; }
    public DbSet<AccountingReport> AccountingReports { get; set; }
    public DbSet<AccountingTransaction> AccountingTransactions { get; set; }
    
    public DbSet<CompanyCategory> CompanyCategories { get; set; }
    public DbSet<Company> Companies { get; set; }
    
    public DbSet<VehicleUnavailablePeriod> VehicleUnavailablePeriods { get; set; }
    Task MigrateAsync();
    
    DatabaseFacade Database { get; }
}