using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PowerSave.Application.Common.Interfaces;
using PowerSave.Infrastructure.Persistence;

namespace PowerSave.Infrastructure.BackgroundServices;

public class UnverifiedUserCleanupBackgroundService : BackgroundService
{
    private const int DefaultRetentionHours = 24;
    private const int DefaultCheckIntervalHours = 1;
    private const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<UnverifiedUserCleanupBackgroundService> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _retentionPeriod;

    public UnverifiedUserCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        IDateTimeProvider dateTimeProvider,
        ILogger<UnverifiedUserCleanupBackgroundService> logger,
        IConfiguration? configuration = null)
    {
        _scopeFactory = scopeFactory;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;

        int retentionHours = configuration?.GetValue<int>("CleanupSettings:UnverifiedAccountRetentionHours") ?? DefaultRetentionHours;
        if (retentionHours <= 0) retentionHours = DefaultRetentionHours;
        _retentionPeriod = TimeSpan.FromHours(retentionHours);

        int intervalHours = configuration?.GetValue<int>("CleanupSettings:CheckIntervalHours") ?? DefaultCheckIntervalHours;
        if (intervalHours <= 0) intervalHours = DefaultCheckIntervalHours;
        _checkInterval = TimeSpan.FromHours(intervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Unverified User Cleanup Service started. Interval: {IntervalHours}h, Retention: {RetentionHours}h.",
            _checkInterval.TotalHours,
            _retentionPeriod.TotalHours);

        using var timer = new PeriodicTimer(_checkInterval);

        try
        {
            // Initial run on startup
            await CleanupStaleAccountsAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CleanupStaleAccountsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Unverified User Cleanup Service is stopping due to host cancellation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during unverified user cleanup background execution.");
        }
    }

    public async Task<int> CleanupStaleAccountsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PowerSaveDbContext>();

        var cutoffTimeUtc = _dateTimeProvider.UtcNow - _retentionPeriod;

        var staleUsers = await dbContext.Users
            .Where(u => !u.PhoneNumberConfirmed && u.CreatedAtUtc < cutoffTimeUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (staleUsers.Count == 0)
        {
            return 0;
        }

        var staleUserIds = staleUsers.Select(u => u.Id).ToList();

        _logger.LogInformation("Found {Count} stale unverified account(s) created before {CutoffUtc}. Purging...",
            staleUsers.Count, cutoffTimeUtc);

        // Explicitly cleanup related records for provider-agnostic consistency
        var relatedOtps = await dbContext.PhoneVerificationOtps
            .Where(o => staleUserIds.Contains(o.UserId))
            .ToListAsync(cancellationToken);

        var relatedConsents = await dbContext.UserConsents
            .Where(c => staleUserIds.Contains(c.UserId))
            .ToListAsync(cancellationToken);

        var relatedLimits = await dbContext.UserOtpRateLimits
            .Where(l => staleUserIds.Contains(l.UserId))
            .ToListAsync(cancellationToken);

        if (relatedOtps.Count > 0) dbContext.PhoneVerificationOtps.RemoveRange(relatedOtps);
        if (relatedConsents.Count > 0) dbContext.UserConsents.RemoveRange(relatedConsents);
        if (relatedLimits.Count > 0) dbContext.UserOtpRateLimits.RemoveRange(relatedLimits);

        dbContext.Users.RemoveRange(staleUsers);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully purged {Count} stale unverified account(s) and associated records.", staleUsers.Count);
        return staleUsers.Count;
    }
}
