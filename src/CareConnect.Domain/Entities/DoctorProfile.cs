namespace CareConnect.Domain.Entities;

public class DoctorProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// One primary specialty per doctor in this MVP. Replaces the free-text field the
    /// first step used, so doctors and hospitals now agree on a single controlled list.
    /// </summary>
    public Guid? SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    public string? LicenseNumber { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Biography { get; set; }

    /// <summary>Fee in EGP. Null means the doctor has not published a price yet.</summary>
    public decimal? ConsultationPrice { get; set; }

    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? ProfileImageUrl { get; set; }

    /// <summary>
    /// Derived server-side only - see <see cref="HasRequiredProfileFields"/>. A doctor must
    /// have a completed profile before they can request a hospital affiliation.
    /// </summary>
    public bool IsProfileCompleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<DoctorHospitalAffiliation> HospitalAffiliations { get; set; } =
        new List<DoctorHospitalAffiliation>();

    /// <summary>
    /// The completion rule, kept next to the data it describes so the API and any future
    /// caller cannot disagree about what "completed" means.
    /// </summary>
    public bool HasRequiredProfileFields(string? fullName) =>
        !string.IsNullOrWhiteSpace(fullName)
        && SpecialtyId.HasValue
        && !string.IsNullOrWhiteSpace(LicenseNumber)
        && YearsOfExperience.HasValue
        && !string.IsNullOrWhiteSpace(Governorate)
        && !string.IsNullOrWhiteSpace(City);
}
