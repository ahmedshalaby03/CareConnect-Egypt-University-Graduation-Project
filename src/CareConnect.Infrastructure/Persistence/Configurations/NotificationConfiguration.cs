using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientApplicationUserId).IsRequired().HasMaxLength(450);
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.Category).HasConversion<string>().HasMaxLength(40);
        builder.Property(n => n.RelatedEntityType).HasConversion<string>().HasMaxLength(50);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.ActionRoute).HasMaxLength(500);
        builder.Property(n => n.DeduplicationKey).HasMaxLength(300);
        builder.Property(n => n.IsRead).HasDefaultValue(false);
        builder.Property(n => n.CreatedAt).IsRequired();

        builder.HasOne(n => n.RecipientApplicationUser)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.RecipientApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => n.RecipientApplicationUserId);
        builder.HasIndex(n => new { n.RecipientApplicationUserId, n.IsRead });
        builder.HasIndex(n => new { n.RecipientApplicationUserId, n.CreatedAt });
        builder.HasIndex(n => n.Category);
        builder.HasIndex(n => n.DeduplicationKey)
            .IsUnique()
            .HasFilter("[DeduplicationKey] IS NOT NULL");
    }
}
