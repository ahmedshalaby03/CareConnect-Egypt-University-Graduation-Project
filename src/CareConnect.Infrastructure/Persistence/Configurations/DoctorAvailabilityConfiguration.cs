using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class DoctorAvailabilityConfiguration : IEntityTypeConfiguration<DoctorAvailability>
{
    public void Configure(EntityTypeBuilder<DoctorAvailability> builder)
    {
        builder.ToTable("DoctorAvailabilities");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.DayOfWeek)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(a => a.SlotDurationMinutes).IsRequired();
        builder.Property(a => a.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(a => a.CreatedAt).IsRequired();

        // Overlap between active blocks is an application-layer check (time ranges cannot
        // be expressed as a database uniqueness constraint), so these are query indexes
        // rather than a unique constraint.
        builder.HasIndex(a => a.DoctorProfileId);
        builder.HasIndex(a => a.HospitalProfileId);
        builder.HasIndex(a => a.DayOfWeek);
        builder.HasIndex(a => a.IsActive);
        builder.HasIndex(a => new { a.DoctorProfileId, a.HospitalProfileId, a.DayOfWeek })
            .HasDatabaseName("IX_DoctorAvailabilities_Doctor_Hospital_Day");

        // Restrict on both sides: a schedule block outlives edits to either profile and
        // must never disappear as a side effect of another delete.
        builder.HasOne(a => a.DoctorProfile)
            .WithMany(d => d.Availabilities)
            .HasForeignKey(a => a.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.HospitalProfile)
            .WithMany(h => h.Availabilities)
            .HasForeignKey(a => a.HospitalProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
