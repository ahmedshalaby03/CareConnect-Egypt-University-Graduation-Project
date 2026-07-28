using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public sealed class MedicalServiceRequestStatusHistoryConfiguration
    : IEntityTypeConfiguration<MedicalServiceRequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<MedicalServiceRequestStatusHistory> builder)
    {
        builder.ToTable("MedicalServiceRequestStatusHistory");
        builder.HasKey(history => history.Id);

        builder.Property(history => history.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(history => history.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(history => history.Reason).HasMaxLength(1000);
        builder.Property(history => history.CreatedAt).IsRequired();

        builder.HasOne(history => history.MedicalServiceRequest)
            .WithMany(request => request.StatusHistory)
            .HasForeignKey(history => history.MedicalServiceRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(history => history.ChangedByApplicationUser)
            .WithMany()
            .HasForeignKey(history => history.ChangedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(history => history.MedicalServiceRequestId);
        builder.HasIndex(history => history.CreatedAt);
    }
}
