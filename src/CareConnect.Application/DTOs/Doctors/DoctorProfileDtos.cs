using CareConnect.Application.DTOs.Specialties;

namespace CareConnect.Application.DTOs.Doctors;

/// <summary>
/// The doctor's own profile. Carries account fields (name, email, phone) copied from
/// ApplicationUser - never the Identity entity itself.
/// </summary>
public class DoctorProfileDto
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }

    public SpecialtyOptionDto? Specialty { get; init; }

    public string? LicenseNumber { get; init; }
    public int? YearsOfExperience { get; init; }
    public string? Biography { get; init; }
    public decimal? ConsultationPrice { get; init; }
    public string? Address { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public string? ProfileImageUrl { get; init; }

    public bool IsProfileCompleted { get; init; }

    /// <summary>Which required fields are still blank, so the UI can tell the doctor what to fix.</summary>
    public IReadOnlyList<string> MissingFields { get; init; } = [];

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class UpdateDoctorProfileRequest
{
    /// <summary>Optional. When supplied, the linked ApplicationUser full name is updated too.</summary>
    public string? FullName { get; set; }

    /// <summary>Optional. When supplied, the linked ApplicationUser phone number is updated too.</summary>
    public string? PhoneNumber { get; set; }

    public Guid? SpecialtyId { get; set; }
    public string? LicenseNumber { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Biography { get; set; }
    public decimal? ConsultationPrice { get; set; }
    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? ProfileImageUrl { get; set; }
}
