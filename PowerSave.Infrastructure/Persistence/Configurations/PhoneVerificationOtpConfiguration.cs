using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PowerSave.Domain.Entities;

namespace PowerSave.Infrastructure.Persistence.Configurations;

public class PhoneVerificationOtpConfiguration : IEntityTypeConfiguration<PhoneVerificationOtp>
{
    public void Configure(EntityTypeBuilder<PhoneVerificationOtp> builder)
    {
        builder.ToTable("PhoneVerificationOtps");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.PhoneNumber)
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(o => o.HashedOtp)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(o => o.Salt)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(o => o.CreatedAtUtc)
            .IsRequired();

        builder.Property(o => o.ExpiresAtUtc)
            .IsRequired();

        builder.Property(o => o.FailedAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(o => o.RowVersion)
            .IsRowVersion();

        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => new { o.UserId, o.CreatedAtUtc });
        builder.HasIndex(o => o.PhoneNumber);
    }
}
