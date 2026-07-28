using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public sealed class MedicalServiceProviderWorkingHourConfiguration
    : IEntityTypeConfiguration<MedicalServiceProviderWorkingHour>
{
    public void Configure(EntityTypeBuilder<MedicalServiceProviderWorkingHour> builder)
    {
        builder.ToTable("MedicalServiceProviderWorkingHours");
        builder.HasKey(hours => hours.Id);

        builder.Property(hours => hours.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(hours => hours.IsClosed).IsRequired();

        builder.HasOne(hours => hours.MedicalServiceProviderProfile)
            .WithMany(profile => profile.WorkingHours)
            .HasForeignKey(hours => hours.MedicalServiceProviderProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(hours => new
            {
                hours.MedicalServiceProviderProfileId,
                hours.DayOfWeek
            })
            .IsUnique()
            .HasDatabaseName("IX_MedicalServiceProviderWorkingHours_Provider_Day_Unique");
    }
}
