using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class DoctorHospitalAffiliationConfiguration : IEntityTypeConfiguration<DoctorHospitalAffiliation>
{
    public void Configure(EntityTypeBuilder<DoctorHospitalAffiliation> builder)
    {
        builder.ToTable("DoctorHospitalAffiliations");

        builder.HasKey(a => a.Id);

        // Stored as text rather than an int so the affiliation history is readable straight
        // out of the database.
        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.RequestedAt).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.ReviewedByUserId).HasMaxLength(450);
        builder.Property(a => a.RejectionReason).HasMaxLength(500);
        builder.Property(a => a.IsPrimary).IsRequired().HasDefaultValue(false);

        builder.Ignore(a => a.BlocksNewRequest);

        // Restrict on both sides: affiliation history outlives any single profile edit and
        // must never disappear as a side effect of another delete.
        builder.HasOne(a => a.DoctorProfile)
            .WithMany(d => d.HospitalAffiliations)
            .HasForeignKey(a => a.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.HospitalProfile)
            .WithMany(h => h.DoctorAffiliations)
            .HasForeignKey(a => a.HospitalProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.DoctorProfileId);
        builder.HasIndex(a => a.HospitalProfileId);
        builder.HasIndex(a => a.Status);

        // Covers the "has this doctor already applied here?" check on every new request.
        builder.HasIndex(a => new { a.DoctorProfileId, a.HospitalProfileId })
            .HasDatabaseName("IX_DoctorHospitalAffiliations_Doctor_Hospital");
    }
}
