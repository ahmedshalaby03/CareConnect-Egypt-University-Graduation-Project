using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public sealed class AppointmentDoctorReviewConfiguration
    : IEntityTypeConfiguration<AppointmentDoctorReview>
{
    public void Configure(EntityTypeBuilder<AppointmentDoctorReview> builder)
    {
        ConfigureCommon(builder, "AppointmentDoctorReviews");
        builder.HasIndex(r => r.AppointmentId).IsUnique();
        builder.HasIndex(r => r.DoctorProfileId);
        builder.HasIndex(r => r.PatientProfileId);
        builder.HasIndex(r => r.ModerationStatus);
        builder.HasOne(r => r.Appointment).WithOne(a => a.DoctorReview)
            .HasForeignKey<AppointmentDoctorReview>(r => r.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.PatientProfile).WithMany(p => p.DoctorReviews)
            .HasForeignKey(r => r.PatientProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.DoctorProfile).WithMany(d => d.Reviews)
            .HasForeignKey(r => r.DoctorProfileId).OnDelete(DeleteBehavior.Restrict);
        ConfigureModerator(builder);
    }

    internal static void ConfigureCommon<TEntity>(EntityTypeBuilder<TEntity> builder, string table)
        where TEntity : class
    {
        builder.ToTable(table, t => t.HasCheckConstraint($"CK_{table}_Rating", "[Rating] BETWEEN 1 AND 5"));
        builder.HasKey("Id");
        builder.Property("Rating").IsRequired();
        builder.Property("Comment").HasMaxLength(2000);
        builder.Property("ModerationReason").HasMaxLength(1000);
        builder.Property("ModerationStatus").HasConversion<string>().HasMaxLength(20);
        builder.Property("CreatedAt").IsRequired();
    }

    internal static void ConfigureModerator<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.HasOne("ModeratedByApplicationUser").WithMany()
            .HasForeignKey("ModeratedByApplicationUserId").OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AppointmentHospitalReviewConfiguration
    : IEntityTypeConfiguration<AppointmentHospitalReview>
{
    public void Configure(EntityTypeBuilder<AppointmentHospitalReview> builder)
    {
        AppointmentDoctorReviewConfiguration.ConfigureCommon(builder, "AppointmentHospitalReviews");
        builder.HasIndex(r => r.AppointmentId).IsUnique();
        builder.HasIndex(r => r.HospitalProfileId);
        builder.HasIndex(r => r.PatientProfileId);
        builder.HasIndex(r => r.ModerationStatus);
        builder.HasOne(r => r.Appointment).WithOne(a => a.HospitalReview)
            .HasForeignKey<AppointmentHospitalReview>(r => r.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.PatientProfile).WithMany(p => p.HospitalReviews)
            .HasForeignKey(r => r.PatientProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.HospitalProfile).WithMany(h => h.Reviews)
            .HasForeignKey(r => r.HospitalProfileId).OnDelete(DeleteBehavior.Restrict);
        AppointmentDoctorReviewConfiguration.ConfigureModerator(builder);
    }
}

public sealed class MedicalServiceProviderReviewConfiguration
    : IEntityTypeConfiguration<MedicalServiceProviderReview>
{
    public void Configure(EntityTypeBuilder<MedicalServiceProviderReview> builder)
    {
        AppointmentDoctorReviewConfiguration.ConfigureCommon(builder, "MedicalServiceProviderReviews");
        builder.HasIndex(r => r.MedicalServiceRequestId).IsUnique();
        builder.HasIndex(r => r.MedicalServiceProviderProfileId);
        builder.HasIndex(r => r.PatientProfileId);
        builder.HasIndex(r => r.ModerationStatus);
        builder.HasOne(r => r.MedicalServiceRequest).WithOne(r => r.Review)
            .HasForeignKey<MedicalServiceProviderReview>(r => r.MedicalServiceRequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.PatientProfile).WithMany(p => p.MedicalServiceProviderReviews)
            .HasForeignKey(r => r.PatientProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.MedicalServiceProviderProfile).WithMany(p => p.Reviews)
            .HasForeignKey(r => r.MedicalServiceProviderProfileId).OnDelete(DeleteBehavior.Restrict);
        AppointmentDoctorReviewConfiguration.ConfigureModerator(builder);
    }
}
