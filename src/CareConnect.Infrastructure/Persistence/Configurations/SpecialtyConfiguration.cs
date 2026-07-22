using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("Specialties");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(s => s.ArabicName).HasMaxLength(120);
        builder.Property(s => s.Description).HasMaxLength(500);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("IX_Specialties_Name_Unique");

        // Arabic name is optional, so the uniqueness rule is filtered: many rows may have
        // no Arabic name, but no two may share one.
        builder.HasIndex(s => s.ArabicName)
            .IsUnique()
            .HasFilter("[ArabicName] IS NOT NULL")
            .HasDatabaseName("IX_Specialties_ArabicName_Unique");

        builder.HasIndex(s => s.IsActive);
    }
}
