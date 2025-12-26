using ClassLibrary1.Configurations;
using Clean.Application.Abstractions;
using Clean.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ClassLibrary1.Data;

public class DataContext : IdentityDbContext<User, IdentityRole<int>, int>, IDataContext
{
    
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
        
    }

    
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

    public async Task MigrateAsync()
    {
        await Database.MigrateAsync();
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(UserConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(DriverAssignmentConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(DriverConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(DriverOffDayConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(DriverVacationConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(FilterConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(ReportPeriodConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(ServiceTypeConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(TripConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(VehicleConfigurations).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(VehicleTypeConfigurations).Assembly);
    }
}