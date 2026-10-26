using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PowerSave.Application.Interfaces;
using PowerSave.Domain.Entities;
using PowerSave.Infrastructure.Persistence;
using PowerSave.Infrastructure.Security;
using PowerSave.Infrastructure.Sms;

namespace PowerSave.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=.;Database=PowerSaveDb;Trusted_Connection=True;TrustServerCertificate=True";

        services.AddDbContext<PowerSaveDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(PowerSaveDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            }));

        services.AddScoped<IPowerSaveDbContext>(sp => sp.GetRequiredService<PowerSaveDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
        {

            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredUniqueChars = 1;

            options.User.RequireUniqueEmail = false;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#_!";

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<PowerSaveDbContext>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IOtpService, OtpService>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));
        var smsOptions = configuration.GetSection(SmsOptions.SectionName).Get<SmsOptions>() ?? new SmsOptions();

        if (smsOptions.Provider.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ISmsSender, ProductionSmsSender>();
        }
        else
        {
            services.AddSingleton<ISmsSender, DevSmsSender>();
        }

        return services;
    }
}

