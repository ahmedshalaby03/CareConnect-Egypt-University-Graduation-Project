using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public sealed class MedicalServiceOfferingConfiguration
    : IEntityTypeConfiguration<MedicalServiceOffering>
{
    public void Configure(EntityTypeBuilder<MedicalServiceOffering> builder)
    {
        builder.ToTable("MedicalServiceOfferings");
        builder.HasKey(service => service.Id);

        builder.Property(service => service.Name).IsRequired().HasMaxLength(150);
        builder.Property(service => service.Description).HasMaxLength(2000);
        builder.Property(service => service.Price).HasPrecision(18, 2);
        builder.Property(service => service.PreparationInstructions).HasMaxLength(2000);
        builder.Property(service => service.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(service => service.CreatedAt).IsRequired();

        builder.HasOne(service => service.MedicalServiceProviderProfile)
            .WithMany(profile => profile.ServiceOfferings)
            .HasForeignKey(service => service.MedicalServiceProviderProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(service => service.MedicalServiceCategory)
            .WithMany(category => category.ServiceOfferings)
            .HasForeignKey(service => service.MedicalServiceCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(service => service.MedicalServiceProviderProfileId);
        builder.HasIndex(service => service.MedicalServiceCategoryId);
        builder.HasIndex(service => service.IsActive);
        builder.HasIndex(service => new
            {
                service.MedicalServiceProviderProfileId,
                service.Name
            })
            .IsUnique()
            .HasDatabaseName("IX_MedicalServiceOfferings_Provider_Name_Unique");
    }
}
