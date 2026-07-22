using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Specialties;
using CareConnect.Domain.Enums;

namespace CareConnect.Application.DTOs.Affiliations;

// ------------------------------------------------------------------- Requests

public class CreateAffiliationRequest
{
    public Guid HospitalProfileId { get; set; }
}

public class RejectAffiliationRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}

// -------------------------------------------------------------------- Queries

public class DoctorAffiliationQueryParameters : PagedQueryParameters
{
    public AffiliationStatus? Status { get; set; }

    /// <summary>Matches the hospital's name.</summary>
    public string? HospitalName { get; set; }
}

public class HospitalAffiliationQueryParameters : PagedQueryParameters
{
    public AffiliationStatus? Status { get; set; }

    /// <summary>Matches the doctor's full name.</summary>
    public string? Search { get; set; }

    public Guid? SpecialtyId { get; set; }
}

// ------------------------------------------------------- Doctor-side responses

/// <summary>One row on the doctor's "my requests" screen.</summary>
public class DoctorHospitalRequestDto
{
    public Guid Id { get; init; }
    public Guid HospitalProfileId { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string? Governorate { get; init; }
    public string? City { get; init; }

    public AffiliationStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;

    public DateTime RequestedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
    public bool IsPrimary { get; init; }
}

/// <summary>A hospital the doctor has been approved to work at.</summary>
public class DoctorAffiliatedHospitalDto
{
    public Guid Id { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public string? PhoneNumber { get; init; }

    public AffiliationStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
}

// ----------------------------------------------------- Hospital-side responses

/// <summary>
/// One row on the hospital's incoming requests screen. Carries only the professional
/// details a hospital needs to make a decision - no private account fields.
/// </summary>
public class HospitalDoctorRequestDto
{
    public Guid Id { get; init; }
    public Guid DoctorProfileId { get; init; }
    public string DoctorName { get; init; } = string.Empty;

    public SpecialtyOptionDto? Specialty { get; init; }

    public string? LicenseNumber { get; init; }
    public int? YearsOfExperience { get; init; }
    public string? Biography { get; init; }
    public string? ProfileImageUrl { get; init; }

    public AffiliationStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;

    public DateTime RequestedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
    public bool IsPrimary { get; init; }
}

/// <summary>A doctor currently approved at the hospital.</summary>
public class HospitalDoctorDto
{
    public Guid AffiliationId { get; init; }
    public Guid DoctorProfileId { get; init; }
    public string DoctorName { get; init; } = string.Empty;

    public SpecialtyOptionDto? Specialty { get; init; }

    public string? LicenseNumber { get; init; }
    public int? YearsOfExperience { get; init; }
    public decimal? ConsultationPrice { get; init; }
    public string? ProfileImageUrl { get; init; }

    public DateTime? ApprovedAt { get; init; }
    public bool IsPrimary { get; init; }
}
