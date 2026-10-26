using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PowerSave.Domain.Entities;
using PowerSave.Infrastructure.Persistence;

namespace PowerSave.Tests.Helpers;

public static class TestAuthDbContextFactory
{
    public static (PowerSaveDbContext DbContext, UserManager<ApplicationUser> UserManager) Create(string dbName)
    {
        var services = new ServiceCollection();

        services.AddLogging(b => b.AddDebug());

        services.AddDbContext<PowerSaveDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

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
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<PowerSaveDbContext>();

        var serviceProvider = services.BuildServiceProvider();

        var dbContext = serviceProvider.GetRequiredService<PowerSaveDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        return (dbContext, userManager);
    }
}

