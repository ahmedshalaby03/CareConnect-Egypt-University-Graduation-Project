using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class BloodRequestConfiguration : IEntityTypeConfiguration<BloodRequest>
{
    public void Configure(EntityTypeBuilder<BloodRequest> builder)
    {
        builder.ToTable("BloodRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.BloodGroup)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.Urgency)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.UnitsRequested).IsRequired();

        builder.Property(r => r.BeneficiaryName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.ContactPhoneNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(r => r.MedicalCondition).HasMaxLength(500);
        builder.Property(r => r.HospitalOrFacilityName).HasMaxLength(200);
        builder.Property(r => r.RequestNotes).HasMaxLength(1000);
        builder.Property(r => r.HospitalNotes).HasMaxLength(1000);
        builder.Property(r => r.RejectionReason).HasMaxLength(500);

        builder.Property(r => r.SubmittedAt).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.Ignore(r => r.IsActiveForDuplicateCheck);

        // Restrict everywhere: blood-request history must survive a profile edit, an
        // account deactivation, or a stock-record change, and each of these is a distinct
        // path back to ApplicationUser - none may be Cascade, or SQL Server would reject
        // the migration for multiple cascade paths.
        builder.HasOne(r => r.PatientProfile)
            .WithMany(p => p.BloodRequests)
            .HasForeignKey(r => r.PatientProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.HospitalProfile)
            .WithMany(h => h.BloodRequests)
            .HasForeignKey(r => r.HospitalProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.BloodStock)
            .WithMany(s => s.BloodRequests)
            .HasForeignKey(r => r.BloodStockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.PatientProfileId);
        builder.HasIndex(r => r.HospitalProfileId);
        builder.HasIndex(r => r.BloodStockId);
        builder.HasIndex(r => r.BloodGroup);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.Urgency);
        builder.HasIndex(r => r.SubmittedAt);

        builder.HasIndex(r => new { r.HospitalProfileId, r.Status })
            .HasDatabaseName("IX_BloodRequests_Hospital_Status");
        builder.HasIndex(r => new { r.PatientProfileId, r.SubmittedAt })
            .HasDatabaseName("IX_BloodRequests_Patient_SubmittedAt");
        builder.HasIndex(r => new { r.HospitalProfileId, r.BloodGroup, r.Status })
            .HasDatabaseName("IX_BloodRequests_Hospital_BloodGroup_Status");
    }
}
