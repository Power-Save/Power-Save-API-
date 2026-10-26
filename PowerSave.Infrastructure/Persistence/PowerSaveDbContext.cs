using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PowerSave.Application.Interfaces;
using PowerSave.Domain.Entities;
using System.Reflection;

namespace PowerSave.Infrastructure.Persistence;

public class PowerSaveDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IPowerSaveDbContext
{
    public PowerSaveDbContext(DbContextOptions<PowerSaveDbContext> options) : base(options)
    {
    }

    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<PhoneVerificationOtp> PhoneVerificationOtps => Set<PhoneVerificationOtp>();
    public DbSet<UserOtpRateLimit> UserOtpRateLimits => Set<UserOtpRateLimit>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

