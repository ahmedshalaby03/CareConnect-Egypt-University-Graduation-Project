using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class InsuranceCompanyConfiguration : IEntityTypeConfiguration<InsuranceCompany>
{
    public void Configure(EntityTypeBuilder<InsuranceCompany> builder)
    {
        builder.ToTable("InsuranceCompanies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.ArabicName).HasMaxLength(150);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.PhoneNumber).HasMaxLength(30);
        builder.Property(c => c.WebsiteUrl).HasMaxLength(500);
        builder.Property(c => c.LogoUrl).HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("IX_InsuranceCompanies_Name_Unique");

        // Arabic name is optional, so the uniqueness rule is filtered: many rows may have
        // no Arabic name, but no two may share one.
        builder.HasIndex(c => c.ArabicName)
            .IsUnique()
            .HasFilter("[ArabicName] IS NOT NULL")
            .HasDatabaseName("IX_InsuranceCompanies_ArabicName_Unique");

        builder.HasIndex(c => c.IsActive);
    }
}
