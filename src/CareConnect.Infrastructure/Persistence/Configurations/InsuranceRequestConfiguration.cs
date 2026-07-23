using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class InsuranceRequestConfiguration : IEntityTypeConfiguration<InsuranceRequest>
{
    public void Configure(EntityTypeBuilder<InsuranceRequest> builder)
    {
        builder.ToTable("InsuranceRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.MemberNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.PolicyNumber).HasMaxLength(100);

        builder.Property(r => r.ServiceDescription)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(r => r.RequestedAmount).HasPrecision(18, 2);
        builder.Property(r => r.ApprovedAmount).HasPrecision(18, 2);

        builder.Property(r => r.PatientNotes).HasMaxLength(2000);
        builder.Property(r => r.HospitalNotes).HasMaxLength(2000);
        builder.Property(r => r.InsuranceCardImageUrl).HasMaxLength(500);
        builder.Property(r => r.SupportingDocumentUrl).HasMaxLength(500);
        builder.Property(r => r.RejectionReason).HasMaxLength(500);
        builder.Property(r => r.ApprovalReferenceNumber).HasMaxLength(100);

        builder.Property(r => r.SubmittedAt).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.Ignore(r => r.BlocksNewRequest);

        // Restrict everywhere: insurance-request history must survive a profile edit, an
        // account deactivation, or an insurance company being deactivated, and each of
        // these is a distinct path back to ApplicationUser - none may be Cascade, or SQL
        // Server would reject the migration for multiple cascade paths.
        builder.HasOne(r => r.PatientProfile)
            .WithMany(p => p.InsuranceRequests)
            .HasForeignKey(r => r.PatientProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.HospitalProfile)
            .WithMany(h => h.InsuranceRequests)
            .HasForeignKey(r => r.HospitalProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Appointment)
            .WithMany(a => a.InsuranceRequests)
            .HasForeignKey(r => r.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.InsuranceCompany)
            .WithMany(c => c.InsuranceRequests)
            .HasForeignKey(r => r.InsuranceCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.PatientProfileId);
        builder.HasIndex(r => r.HospitalProfileId);
        builder.HasIndex(r => r.AppointmentId);
        builder.HasIndex(r => r.InsuranceCompanyId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.SubmittedAt);

        builder.HasIndex(r => new { r.AppointmentId, r.Status })
            .HasDatabaseName("IX_InsuranceRequests_Appointment_Status");
        builder.HasIndex(r => new { r.HospitalProfileId, r.Status })
            .HasDatabaseName("IX_InsuranceRequests_Hospital_Status");
        builder.HasIndex(r => new { r.PatientProfileId, r.SubmittedAt })
            .HasDatabaseName("IX_InsuranceRequests_Patient_SubmittedAt");

        // Last line of defence against the duplicate-request race: at most one
        // Pending/UnderReview/Approved row may exist per appointment. Rejected and
        // Cancelled rows are excluded from the filter, so a resubmission after either is
        // unaffected.
        builder.HasIndex(r => r.AppointmentId)
            .IsUnique()
            .HasFilter("[Status] IN ('Pending', 'UnderReview', 'Approved')")
            .HasDatabaseName("IX_InsuranceRequests_Appointment_ActiveUnique");
    }
}
