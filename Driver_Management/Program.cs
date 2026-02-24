using System.Text;
using System.Text.Json.Serialization;
using ClassLibrary1;
using ClassLibrary1.Data.Seeds;
using Clean.Application;
using Clean.Application.Abstractions;
using Clean.Application.Security.Permission;
using Driver_Management.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Json;

namespace Driver_Management;

/// <summary>
/// Entry point for the Driver Management API application.
/// Configures services, middleware, and runs the web host.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main method for the application.
    /// </summary>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ---------------- LOGGING ----------------
        Directory.CreateDirectory("logs");

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                new JsonFormatter(),
                path: "logs/log-.json",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();

        builder.Host.UseSerilog();

        // ---------------- MVC + API ----------------
        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // ---------------- SWAGGER ----------------
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Driver Scheduling API",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Enter your JWT token",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        
        // -------------------- ENUM Fix --------------------

        
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        // -------------------- CORS --------------------
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(
                        "http://192.168.68.123",
                        "http://192.168.68.115",
                        "http://192.168.68.123:5173",  // Vite dev server
                        "http://192.168.68.123:3000",  // React dev server
                        "http://localhost:5173",
                        "http://localhost:3000"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        // ---------------- AUTH ----------------
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                string key = builder.Configuration["JWT:Key"] 
                             ?? throw new InvalidOperationException("JWT:Key is missing in configuration");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JWT:Issuer"],

                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JWT:Audience"],

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key))
                };
            });

        // ---------------- PERMISSIONS ----------------
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // ---------------- Clean Architecture ----------------
        builder.Services.AddScoped<LoggingMiddleware>();
        builder.Services.AddIdentityServices(builder.Configuration);
        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddInfrastructureServices(builder.Configuration);

        // ---------------- BUILD APP ----------------
        var app = builder.Build();

        // ------------ Migrations & Seed Data ------------
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IDataContext>();
            await db.MigrateAsync();

            var seeder = scope.ServiceProvider.GetRequiredService<SeedDataInitializer>();
            await seeder.InitializeAsync();
        }

        // ------------ MIDDLEWARE ------------

        // Then use it:
        app.UseCors("AllowFrontend");
        
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = string.Empty;
        });


        app.UseMiddleware<LoggingMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        await app.RunAsync();
    }
}