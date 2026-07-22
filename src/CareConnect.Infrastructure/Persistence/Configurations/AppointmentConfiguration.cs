using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.PatientNotes).HasMaxLength(2000);
        builder.Property(a => a.DoctorNotes).HasMaxLength(4000);
        builder.Property(a => a.RejectionReason).HasMaxLength(500);
        builder.Property(a => a.CancellationReason).HasMaxLength(500);
        builder.Property(a => a.CancelledByUserId).HasMaxLength(450);
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.Ignore(a => a.BlocksSlot);

        // Restrict everywhere: appointment history must survive a profile edit or an
        // account deactivation, and every one of these is a distinct path back to
        // ApplicationUser, so none of them may be Cascade (that would create the multiple
        // cascade-path error SQL Server rejects at migration time).
        builder.HasOne(a => a.PatientProfile)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.DoctorProfile)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.HospitalProfile)
            .WithMany(h => h.Appointments)
            .HasForeignKey(a => a.HospitalProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CancelledByUser)
            .WithMany()
            .HasForeignKey(a => a.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.PatientProfileId);
        builder.HasIndex(a => a.DoctorProfileId);
        builder.HasIndex(a => a.HospitalProfileId);
        builder.HasIndex(a => a.AppointmentDate);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => new { a.DoctorProfileId, a.AppointmentDate })
            .HasDatabaseName("IX_Appointments_Doctor_Date");
        builder.HasIndex(a => new { a.HospitalProfileId, a.AppointmentDate })
            .HasDatabaseName("IX_Appointments_Hospital_Date");
        builder.HasIndex(a => new { a.DoctorProfileId, a.AppointmentDate, a.StartTime })
            .HasDatabaseName("IX_Appointments_Doctor_Date_StartTime");

        // Last line of defence against a double booking race: two Pending/Confirmed rows
        // for the same doctor, date and slot start cannot both exist, so if two requests
        // for the identical slot reach SaveChanges together, the second fails the
        // constraint and the service turns that into a 409 rather than a 500.
        builder.HasIndex(a => new { a.DoctorProfileId, a.AppointmentDate, a.StartTime })
            .IsUnique()
            .HasFilter("[Status] IN ('Pending', 'Confirmed')")
            .HasDatabaseName("IX_Appointments_Doctor_Date_StartTime_ActiveUnique");
    }
}
