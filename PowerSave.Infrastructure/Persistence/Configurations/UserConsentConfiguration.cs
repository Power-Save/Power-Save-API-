using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PowerSave.Domain.Entities;

namespace PowerSave.Infrastructure.Persistence.Configurations;

public class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.ToTable("UserConsents");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.DocumentVersion)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.PolicyIdentifier)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.AcceptedAtUtc)
            .IsRequired();

        builder.Property(c => c.ClientIpAddress)
            .HasMaxLength(45);

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => new { c.UserId, c.ConsentType });
    }
}

