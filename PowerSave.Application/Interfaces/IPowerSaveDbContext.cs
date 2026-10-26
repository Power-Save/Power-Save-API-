using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PowerSave.Domain.Entities;

namespace PowerSave.Application.Interfaces;

public interface IPowerSaveDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<UserConsent> UserConsents { get; }
    DbSet<PhoneVerificationOtp> PhoneVerificationOtps { get; }
    DbSet<UserOtpRateLimit> UserOtpRateLimits { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

