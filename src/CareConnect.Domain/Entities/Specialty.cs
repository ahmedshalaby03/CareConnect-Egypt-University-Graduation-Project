namespace CareConnect.Domain.Entities;

/// <summary>
/// A medical specialty, managed centrally by the SuperAdmin so doctors and hospitals pick
/// from one controlled list instead of typing free text.
///
/// Specialties are never deleted. Deactivating one hides it from selection while leaving
/// every profile that already references it intact.
/// </summary>
public class Specialty
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>Optional Arabic label shown alongside the English name in the UI.</summary>
    public string? ArabicName { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<DoctorProfile> DoctorProfiles { get; set; } = new List<DoctorProfile>();
    public ICollection<HospitalSpecialty> HospitalSpecialties { get; set; } = new List<HospitalSpecialty>();
}
