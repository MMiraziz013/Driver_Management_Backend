using Clean.Application.Abstractions;
using Clean.Application.Services.Driver;
using Clean.Application.Services.DriverVacation;
using Clean.Application.Services.Gas;
using Clean.Application.Services.JWT;
using Clean.Application.Services.Mapbox;
using Clean.Application.Services.Report;
using Clean.Application.Services.ReportPeriod;
using Clean.Application.Services.ServiceType;
using Clean.Application.Services.Trip;
using Clean.Application.Services.User;
using Clean.Application.Services.Vehicle;
using Clean.Application.Services.VehicleType;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clean.Application;

public static class ApplicationInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        //TODO: Test the VehicleType Service and implement VehicleType repository.
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IDriverService, DriverService>();
        services.AddTransient<IReportPeriodService, ReportPeriodService>();
        services.AddTransient<IVehicleTypeService, VehicleTypeService>();
        services.AddTransient<IVehicleService, VehicleService>();
        services.AddTransient<ITripService, TripService>();
        services.AddTransient<IReportService, ReportService>();
        services.AddTransient<IServiceTypeService, ServiceTypeService>();
        services.AddScoped<IGasService, GasService>();
        services.AddScoped<IDriverVacationService, DriverVacationService>();

        
        services.AddHttpClient<IMapboxService, MapboxService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IMapboxService, MapboxService>();
        
        services.AddTransient<IJwtTokenService, JwtTokenService>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<JwtTokenService>(configuration.GetSection(JwtOptions.SectionName));


        return services;
    }
}