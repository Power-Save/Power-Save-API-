using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PowerSave.Domain.Entities;
using PowerSave.Domain.Enums;
using PowerSave.Infrastructure.BackgroundServices;
using PowerSave.Infrastructure.Persistence;
using PowerSave.Tests.Helpers;
using Xunit;

namespace PowerSave.Tests.Authentication;

public class CleanupServiceTests
{
    private readonly TestTimeProvider _timeProvider = new();

    private (UnverifiedUserCleanupBackgroundService service, PowerSaveDbContext dbContext) CreateService(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<PowerSaveDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: dbName));

        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var dbContext = sp.GetRequiredService<PowerSaveDbContext>();

        var service = new UnverifiedUserCleanupBackgroundService(
            scopeFactory,
            _timeProvider,
            NullLogger<UnverifiedUserCleanupBackgroundService>.Instance);

        return (service, dbContext);
    }

    [Fact]
    public async Task CleanupStaleAccounts_UnverifiedUserOlderThan24Hours_ShouldBeDeleted()
    {
        var dbName = Guid.NewGuid().ToString();
        var (service, dbContext) = CreateService(dbName);

        var now = _timeProvider.UtcNow;
        var staleUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "StaleUser",
            PhoneNumber = "01011112222",
            PhoneNumberConfirmed = false,
            IsActive = false,
            CreatedAtUtc = now.AddHours(-25) // 25 hours ago
        };

        dbContext.Users.Add(staleUser);
        await dbContext.SaveChangesAsync();

        var purgedCount = await service.CleanupStaleAccountsAsync();

        Assert.Equal(1, purgedCount);
        var remainingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == staleUser.Id);
        Assert.Null(remainingUser);
    }

    [Fact]
    public async Task CleanupStaleAccounts_UnverifiedUserNewerThan24Hours_ShouldBePreserved()
    {
        var dbName = Guid.NewGuid().ToString();
        var (service, dbContext) = CreateService(dbName);

        var now = _timeProvider.UtcNow;
        var recentUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "RecentUser",
            PhoneNumber = "01033334444",
            PhoneNumberConfirmed = false,
            IsActive = false,
            CreatedAtUtc = now.AddHours(-2) // Only 2 hours ago
        };

        dbContext.Users.Add(recentUser);
        await dbContext.SaveChangesAsync();

        var purgedCount = await service.CleanupStaleAccountsAsync();

        Assert.Equal(0, purgedCount);
        var preservedUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == recentUser.Id);
        Assert.NotNull(preservedUser);
    }

    [Fact]
    public async Task CleanupStaleAccounts_VerifiedUserOlderThan24Hours_ShouldBePreserved()
    {
        var dbName = Guid.NewGuid().ToString();
        var (service, dbContext) = CreateService(dbName);

        var now = _timeProvider.UtcNow;
        var activeUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "ActiveUser",
            PhoneNumber = "01055556666",
            PhoneNumberConfirmed = true, // Verified!
            IsActive = true,
            CreatedAtUtc = now.AddDays(-10) // 10 days ago
        };

        dbContext.Users.Add(activeUser);
        await dbContext.SaveChangesAsync();

        var purgedCount = await service.CleanupStaleAccountsAsync();

        Assert.Equal(0, purgedCount);
        var preservedUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == activeUser.Id);
        Assert.NotNull(preservedUser);
    }

    [Fact]
    public async Task CleanupStaleAccounts_RelatedEntities_ShouldBeRemovedWithStaleUser()
    {
        var dbName = Guid.NewGuid().ToString();
        var (service, dbContext) = CreateService(dbName);

        var now = _timeProvider.UtcNow;
        var staleUserId = Guid.NewGuid();

        var staleUser = new ApplicationUser
        {
            Id = staleUserId,
            UserName = "CascadeUser",
            PhoneNumber = "01077778888",
            PhoneNumberConfirmed = false,
            IsActive = false,
            CreatedAtUtc = now.AddHours(-48)
        };

        var otp = new PhoneVerificationOtp
        {
            Id = Guid.NewGuid(),
            UserId = staleUserId,
            PhoneNumber = "01077778888",
            HashedOtp = "hash",
            Salt = "salt",
            CreatedAtUtc = now.AddHours(-48),
            ExpiresAtUtc = now.AddHours(-48).AddMinutes(5)
        };

        var consent = new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = staleUserId,
            ConsentType = ConsentType.AccountCreationAndDataCollection,
            IsAccepted = true,
            DocumentVersion = "v1.0",
            PolicyIdentifier = "policy-v1",
            AcceptedAtUtc = now.AddHours(-48)
        };

        var rateLimit = new UserOtpRateLimit
        {
            UserId = staleUserId,
            LastOtpSentAtUtc = now.AddHours(-48),
            ResendAttemptsInCurrentBatch = 1,
            BatchIndex = 1
        };

        dbContext.Users.Add(staleUser);
        dbContext.PhoneVerificationOtps.Add(otp);
        dbContext.UserConsents.Add(consent);
        dbContext.UserOtpRateLimits.Add(rateLimit);
        await dbContext.SaveChangesAsync();

        var purgedCount = await service.CleanupStaleAccountsAsync();

        Assert.Equal(1, purgedCount);
        Assert.False(await dbContext.Users.AnyAsync(u => u.Id == staleUserId));
        Assert.False(await dbContext.PhoneVerificationOtps.AnyAsync(o => o.UserId == staleUserId));
        Assert.False(await dbContext.UserConsents.AnyAsync(c => c.UserId == staleUserId));
        Assert.False(await dbContext.UserOtpRateLimits.AnyAsync(r => r.UserId == staleUserId));
    }
}
