using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt).IsRequired();

        // Identity already gives us a unique index on NormalizedEmail. Phone numbers are
        // optional, so this one is filtered: many users may have no phone, only one may
        // have any given phone.
        builder.HasIndex(u => u.PhoneNumber)
            .IsUnique()
            .HasFilter("[PhoneNumber] IS NOT NULL")
            .HasDatabaseName("IX_AspNetUsers_PhoneNumber_Unique");

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(t => t.User!)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-one, one per role. A user only ever has the profile matching their role.
        builder.HasOne(u => u.PatientProfile)
            .WithOne(p => p.User!)
            .HasForeignKey<PatientProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.DoctorProfile)
            .WithOne(p => p.User!)
            .HasForeignKey<DoctorProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.HospitalProfile)
            .WithOne(p => p.User!)
            .HasForeignKey<HospitalProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.MedicalServiceProviderProfile)
            .WithOne(p => p.User!)
            .HasForeignKey<MedicalServiceProviderProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
