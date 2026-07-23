using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
{
    public void Configure(EntityTypeBuilder<PatientProfile> builder)
    {
        builder.ToTable("PatientProfiles");
        builder.HasKey(p => p.Id);

        // Enforces the one-to-one from the dependent side as well.
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.Gender).HasMaxLength(20);
        builder.Property(p => p.Address).HasMaxLength(400);
    }
}

public class DoctorProfileConfiguration : IEntityTypeConfiguration<DoctorProfile>
{
    public void Configure(EntityTypeBuilder<DoctorProfile> builder)
    {
        builder.ToTable("DoctorProfiles");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.LicenseNumber).HasMaxLength(100);
        builder.Property(p => p.Biography).HasMaxLength(2000);
        builder.Property(p => p.Address).HasMaxLength(400);
        builder.Property(p => p.Governorate).HasMaxLength(100);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.ProfileImageUrl).HasMaxLength(500);

        builder.Property(p => p.ConsultationPrice).HasPrecision(10, 2);

        builder.Property(p => p.IsProfileCompleted).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).IsRequired();

        // Restrict: a doctor pointing at a specialty must never allow that specialty to be
        // deleted out from under them. Specialties are deactivated, never removed.
        builder.HasOne(p => p.Specialty)
            .WithMany(s => s.DoctorProfiles)
            .HasForeignKey(p => p.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.SpecialtyId);

        // The directory filters on these three together.
        builder.HasIndex(p => new { p.IsProfileCompleted, p.Governorate, p.City })
            .HasDatabaseName("IX_DoctorProfiles_Completed_Location");
    }
}

public class HospitalProfileConfiguration : IEntityTypeConfiguration<HospitalProfile>
{
    public void Configure(EntityTypeBuilder<HospitalProfile> builder)
    {
        builder.ToTable("HospitalProfiles");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.HospitalName).HasMaxLength(200);
        builder.Property(p => p.Address).HasMaxLength(400);
        builder.Property(p => p.Governorate).HasMaxLength(100);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.PhoneNumber).HasMaxLength(30);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.LogoUrl).HasMaxLength(500);
        builder.Property(p => p.WebsiteUrl).HasMaxLength(500);
        builder.Property(p => p.LocationDescription).HasMaxLength(500);
        builder.Property(p => p.NearbyLandmark).HasMaxLength(200);

        builder.Property(p => p.IsProfileCompleted).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).IsRequired();

        // ~11 cm of precision - plenty for straight-line distance search.
        builder.Property(p => p.Latitude).HasPrecision(9, 6);
        builder.Property(p => p.Longitude).HasPrecision(9, 6);

        // The directory filters on these three together.
        builder.HasIndex(p => new { p.IsProfileCompleted, p.Governorate, p.City })
            .HasDatabaseName("IX_HospitalProfiles_Completed_Location");

        builder.HasIndex(p => p.Governorate);
        builder.HasIndex(p => p.City);

        // Narrows the bounding-box pre-filter in nearby search before Haversine runs in memory.
        builder.HasIndex(p => new { p.Latitude, p.Longitude })
            .HasDatabaseName("IX_HospitalProfiles_Latitude_Longitude");
    }
}

public class MedicalServiceProviderProfileConfiguration
    : IEntityTypeConfiguration<MedicalServiceProviderProfile>
{
    public void Configure(EntityTypeBuilder<MedicalServiceProviderProfile> builder)
    {
        builder.ToTable("MedicalServiceProviderProfiles");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.ProviderName).HasMaxLength(200);
        builder.Property(p => p.ServiceType).HasMaxLength(100);
        builder.Property(p => p.Address).HasMaxLength(400);
        builder.Property(p => p.Governorate).HasMaxLength(100);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(2000);
    }
}
