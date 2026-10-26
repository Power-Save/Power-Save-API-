using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PowerSave.Domain.Entities;

namespace PowerSave.Infrastructure.Persistence.Configurations;

public class UserOtpRateLimitConfiguration : IEntityTypeConfiguration<UserOtpRateLimit>
{
    public void Configure(EntityTypeBuilder<UserOtpRateLimit> builder)
    {
        builder.ToTable("UserOtpRateLimits");

        builder.HasKey(r => r.UserId);

        builder.Property(r => r.LastOtpSentAtUtc)
            .IsRequired();

        builder.Property(r => r.RowVersion)
            .IsRowVersion();
    }
}

