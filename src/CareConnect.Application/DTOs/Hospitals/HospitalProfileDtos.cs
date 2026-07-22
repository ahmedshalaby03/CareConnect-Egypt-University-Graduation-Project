using CareConnect.Application.DTOs.Specialties;

namespace CareConnect.Application.DTOs.Hospitals;

public class HospitalProfileDto
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? AccountPhoneNumber { get; init; }

    public string? HospitalName { get; init; }
    public string? Address { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public string? WebsiteUrl { get; init; }

    /// <summary>Serialised as "HH:mm".</summary>
    public string? OpeningTime { get; init; }
    public string? ClosingTime { get; init; }

    public bool IsProfileCompleted { get; init; }
    public IReadOnlyList<string> MissingFields { get; init; } = [];

    public IReadOnlyList<SpecialtyOptionDto> Specialties { get; init; } = [];

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class UpdateHospitalProfileRequest
{
    /// <summary>Optional. Updates the linked ApplicationUser display name when supplied.</summary>
    public string? FullName { get; set; }

    public string? HospitalName { get; set; }
    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }

    /// <summary>"HH:mm" or null.</summary>
    public string? OpeningTime { get; set; }
    public string? ClosingTime { get; set; }
}

public class UpdateHospitalSpecialtiesRequest
{
    /// <summary>The complete desired set. The stored collection is replaced to match it.</summary>
    public List<Guid> SpecialtyIds { get; set; } = [];
}
