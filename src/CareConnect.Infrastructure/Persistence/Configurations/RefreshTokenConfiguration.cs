using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        // Base64 of a SHA-256 digest is always 44 characters.
        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(88);

        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(88);
        builder.Property(t => t.CreatedByIp).HasMaxLength(64);
        builder.Property(t => t.RevokedByIp).HasMaxLength(64);
        builder.Property(t => t.RevokedReason).HasMaxLength(256);

        builder.Property(t => t.UserId).IsRequired();

        // Every refresh lands here, so this index carries the hot path.
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsRevoked);
        builder.Ignore(t => t.IsActive);
    }
}
