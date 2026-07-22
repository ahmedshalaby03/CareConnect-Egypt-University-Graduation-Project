using CareConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareConnect.Infrastructure.Persistence.Configurations;

public class HospitalSpecialtyConfiguration : IEntityTypeConfiguration<HospitalSpecialty>
{
    public void Configure(EntityTypeBuilder<HospitalSpecialty> builder)
    {
        builder.ToTable("HospitalSpecialties");

        // Composite key: a hospital lists a given specialty at most once. This doubles as
        // the unique index the requirement asks for.
        builder.HasKey(hs => new { hs.HospitalProfileId, hs.SpecialtyId });

        builder.HasIndex(hs => new { hs.HospitalProfileId, hs.SpecialtyId })
            .IsUnique()
            .HasDatabaseName("IX_HospitalSpecialties_Hospital_Specialty_Unique");

        builder.HasIndex(hs => hs.SpecialtyId);

        builder.Property(hs => hs.CreatedAt).IsRequired();

        // Cascade from the hospital: these rows belong to the hospital and mean nothing
        // without it.
        builder.HasOne(hs => hs.HospitalProfile)
            .WithMany(h => h.HospitalSpecialties)
            .HasForeignKey(hs => hs.HospitalProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict from the specialty: dropping a hospital's specialty must never reach
        // through and delete the Specialty itself.
        builder.HasOne(hs => hs.Specialty)
            .WithMany(s => s.HospitalSpecialties)
            .HasForeignKey(hs => hs.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
