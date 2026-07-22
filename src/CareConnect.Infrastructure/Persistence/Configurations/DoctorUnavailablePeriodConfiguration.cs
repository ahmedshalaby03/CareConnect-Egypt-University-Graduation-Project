using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class DoctorUnavailablePeriodConfiguration : IEntityTypeConfiguration<DoctorUnavailablePeriod>
{
    public void Configure(EntityTypeBuilder<DoctorUnavailablePeriod> builder)
    {
        builder.ToTable("DoctorUnavailablePeriods");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.StartDateTime).IsRequired();
        builder.Property(p => p.EndDateTime).IsRequired();
        builder.Property(p => p.Reason).HasMaxLength(500);
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => p.DoctorProfileId);
        builder.HasIndex(p => p.HospitalProfileId);
        builder.HasIndex(p => p.StartDateTime);
        builder.HasIndex(p => p.EndDateTime);

        builder.HasOne(p => p.DoctorProfile)
            .WithMany(d => d.UnavailablePeriods)
            .HasForeignKey(p => p.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // No inverse collection on HospitalProfile: an unavailable period is a doctor's own
        // record, and the hospital never needs to enumerate them directly.
        builder.HasOne(p => p.HospitalProfile)
            .WithMany()
            .HasForeignKey(p => p.HospitalProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
