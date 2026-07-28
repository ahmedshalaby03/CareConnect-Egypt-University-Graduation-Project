using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public sealed class MedicalServiceCategoryConfiguration
    : IEntityTypeConfiguration<MedicalServiceCategory>
{
    public void Configure(EntityTypeBuilder<MedicalServiceCategory> builder)
    {
        builder.ToTable("MedicalServiceCategories");
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name).IsRequired().HasMaxLength(120);
        builder.Property(category => category.Description).HasMaxLength(1000);
        builder.Property(category => category.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(category => category.CreatedAt).IsRequired();

        // SQL Server's application collation is case-insensitive, so this protects the
        // natural key without adding a duplicate normalised-name column.
        builder.HasIndex(category => category.Name).IsUnique();
        builder.HasIndex(category => category.IsActive);
    }
}
