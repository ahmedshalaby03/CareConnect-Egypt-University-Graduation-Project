namespace CareConnect.Domain.Entities;

/// <summary>
/// Join entity for the many-to-many between hospitals and specialties. Explicit rather than
/// implicit so the row carries its own CreatedAt and can grow extra columns later.
/// </summary>
public class HospitalSpecialty
{
    public Guid HospitalProfileId { get; set; }
    public HospitalProfile? HospitalProfile { get; set; }

    public Guid SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
