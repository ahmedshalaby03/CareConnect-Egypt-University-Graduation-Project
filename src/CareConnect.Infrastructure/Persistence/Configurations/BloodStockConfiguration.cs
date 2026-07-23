using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class BloodStockConfiguration : IEntityTypeConfiguration<BloodStock>
{
    public void Configure(EntityTypeBuilder<BloodStock> builder)
    {
        builder.ToTable("BloodStocks");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.BloodGroup)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.AvailableUnits).IsRequired();
        builder.Property(s => s.MinimumRequiredUnits).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.IsAvailable).IsRequired();
        builder.Property(s => s.LastUpdatedByUserId).HasMaxLength(450);
        builder.Property(s => s.CreatedAt).IsRequired();

        builder.Ignore(s => s.IsBelowMinimum);

        // Restrict: stock history must survive a hospital account deactivation, and
        // LastUpdatedByUser is a second, distinct path back to ApplicationUser - neither
        // may be Cascade, or SQL Server would reject the migration for multiple cascade paths.
        builder.HasOne(s => s.HospitalProfile)
            .WithMany(h => h.BloodStocks)
            .HasForeignKey(s => s.HospitalProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.LastUpdatedByUser)
            .WithMany()
            .HasForeignKey(s => s.LastUpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.HospitalProfileId);
        builder.HasIndex(s => s.BloodGroup);
        builder.HasIndex(s => s.IsAvailable);
        builder.HasIndex(s => s.AvailableUnits);

        // At most one stock row per hospital and blood group.
        builder.HasIndex(s => new { s.HospitalProfileId, s.BloodGroup })
            .IsUnique()
            .HasDatabaseName("IX_BloodStocks_Hospital_BloodGroup_Unique");
    }
}
