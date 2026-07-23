namespace CareConnect.Domain.Entities;

public class HospitalProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string? HospitalName { get; set; }
    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }

    // Captured now so the map feature planned for a later step has somewhere to write.
    // Nothing in this step reads them.
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    /// <summary>Public switchboard number, which is not necessarily the account's login phone.</summary>
    public string? PhoneNumber { get; set; }

    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }

    public TimeOnly? OpeningTime { get; set; }
    public TimeOnly? ClosingTime { get; set; }

    /// <summary>
    /// Derived server-side only - see <see cref="HasRequiredProfileFields"/>. A hospital
    /// must have a completed profile before it can receive new doctor requests, and only
    /// completed hospitals appear in the public directory.
    /// </summary>
    public bool IsProfileCompleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<HospitalSpecialty> HospitalSpecialties { get; set; } =
        new List<HospitalSpecialty>();

    public ICollection<DoctorHospitalAffiliation> DoctorAffiliations { get; set; } =
        new List<DoctorHospitalAffiliation>();

    public ICollection<DoctorAvailability> Availabilities { get; set; } =
        new List<DoctorAvailability>();

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<InsuranceRequest> InsuranceRequests { get; set; } = new List<InsuranceRequest>();
    public ICollection<BloodStock> BloodStocks { get; set; } = new List<BloodStock>();
    public ICollection<BloodRequest> BloodRequests { get; set; } = new List<BloodRequest>();

    public bool HasRequiredProfileFields() =>
        !string.IsNullOrWhiteSpace(HospitalName)
        && !string.IsNullOrWhiteSpace(Address)
        && !string.IsNullOrWhiteSpace(Governorate)
        && !string.IsNullOrWhiteSpace(City)
        && !string.IsNullOrWhiteSpace(PhoneNumber);
}
