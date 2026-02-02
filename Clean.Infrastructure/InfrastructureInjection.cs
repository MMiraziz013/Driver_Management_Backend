using ClassLibrary1.Data;
using ClassLibrary1.Data.Repositories;
using ClassLibrary1.Data.Seeds;
using Clean.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClassLibrary1;

public static class InfrastructureInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        
        var baseConnectionString = configuration.GetConnectionString("DefaultConnection");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(baseConnectionString);

        if (!string.IsNullOrEmpty(dbPassword))
        {
            builder.Password = dbPassword;
        }

        services.AddDbContext<DataContext>(options =>
            options.UseNpgsql(builder.ConnectionString));

        services.AddScoped<IDataContext>(provider => provider.GetRequiredService<DataContext>());

        services.AddTransient<IDataSeeder, IdentitySeeder>();
        services.AddTransient<IDataSeeder, SeedAdminUser>();
        services.AddTransient<SeedDataInitializer>();

        services.AddScoped<ICachedLocationRepository, CachedLocationRepository>();

        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IDriverRepository, DriverRepository>();
        services.AddTransient<ITripRepository, TripRepository>();
        services.AddTransient<IVehicleRepository, VehicleRepository>();
        services.AddTransient<IVehicleTypeRepository, VehicleTypeRepository>();
        services.AddTransient<IReportPeriodRepository, ReportPeriodRepository>();
        services.AddTransient<IServiceTypeRepository, ServiceTypeRepository>();
        services.AddTransient<IGasPurchaseRepository, GasPurchaseRepository>();
        services.AddTransient<IVehicleFuelAllocationRepository, VehicleFuelAllocationRepository>();
        services.AddTransient<IDriverPeriodStateRepository, DriverPeriodStateRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}