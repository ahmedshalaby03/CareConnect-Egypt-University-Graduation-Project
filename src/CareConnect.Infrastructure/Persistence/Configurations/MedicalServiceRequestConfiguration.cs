using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public sealed class MedicalServiceRequestConfiguration
    : IEntityTypeConfiguration<MedicalServiceRequest>
{
    public void Configure(EntityTypeBuilder<MedicalServiceRequest> builder)
    {
        builder.ToTable("MedicalServiceRequests");
        builder.HasKey(request => request.Id);

        builder.Property(request => request.RequestNumber).IsRequired().HasMaxLength(25);
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(request => request.DeliveryMode).HasConversion<string>().HasMaxLength(30);
        builder.Property(request => request.PatientNotes).HasMaxLength(2000);
        builder.Property(request => request.HomeVisitAddress).HasMaxLength(500);
        builder.Property(request => request.ProviderResponseNote).HasMaxLength(2000);
        builder.Property(request => request.RejectionReason).HasMaxLength(1000);
        builder.Property(request => request.CancellationReason).HasMaxLength(1000);
        builder.Property(request => request.ServiceNameSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(request => request.CategoryNameSnapshot).IsRequired().HasMaxLength(150);
        builder.Property(request => request.PriceSnapshot).HasPrecision(18, 2);
        builder.Property(request => request.CreatedAt).IsRequired();
        builder.Property(request => request.RowVersion).IsRowVersion();

        builder.HasOne(request => request.PatientProfile)
            .WithMany(profile => profile.MedicalServiceRequests)
            .HasForeignKey(request => request.PatientProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.MedicalServiceProviderProfile)
            .WithMany(profile => profile.MedicalServiceRequests)
            .HasForeignKey(request => request.MedicalServiceProviderProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.MedicalServiceOffering)
            .WithMany(offering => offering.MedicalServiceRequests)
            .HasForeignKey(request => request.MedicalServiceOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(request => request.RequestNumber).IsUnique();
        builder.HasIndex(request => request.PatientProfileId);
        builder.HasIndex(request => request.MedicalServiceProviderProfileId);
        builder.HasIndex(request => request.MedicalServiceOfferingId);
        builder.HasIndex(request => request.Status);
        builder.HasIndex(request => request.RequestedDate);
        builder.HasIndex(request => request.ScheduledAt);

        // Last-line race guard for repeated clicks or concurrent identical submissions.
        builder.HasIndex(request => new
            {
                request.PatientProfileId,
                request.MedicalServiceOfferingId,
                request.RequestedDate,
                request.PreferredStartTime
            })
            .IsUnique()
            .HasFilter("[Status] IN ('Pending', 'Accepted')")
            .HasDatabaseName("IX_MedicalServiceRequests_Patient_Service_Date_Time_ActiveUnique");
    }
}
