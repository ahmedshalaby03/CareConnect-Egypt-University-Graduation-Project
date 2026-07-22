using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Specialties;

namespace CareConnect.Application.DTOs.Directory;

// -------------------------------------------------------------------- Queries

public class HospitalDirectoryQueryParameters : PagedQueryParameters
{
    /// <summary>Matches the hospital name or description.</summary>
    public string? Search { get; set; }

    public string? Governorate { get; set; }
    public string? City { get; set; }
    public Guid? SpecialtyId { get; set; }
}

public class DoctorDirectoryQueryParameters : PagedQueryParameters
{
    /// <summary>Matches the doctor's full name or biography.</summary>
    public string? Search { get; set; }

    public Guid? SpecialtyId { get; set; }

    /// <summary>Restricts to doctors approved at this hospital.</summary>
    public Guid? HospitalId { get; set; }

    public string? Governorate { get; set; }
    public string? City { get; set; }
}

// ----------------------------------------------------------- Hospital results

public class HospitalDirectoryItemDto
{
    public Guid Id { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Description { get; init; }
    public string? LogoUrl { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }

    public IReadOnlyList<SpecialtyOptionDto> Specialties { get; init; } = [];

    public int NumberOfApprovedDoctors { get; init; }
}

public class HospitalDirectoryDetailsDto : HospitalDirectoryItemDto
{
    public string? WebsiteUrl { get; init; }
    public string? OpeningTime { get; init; }
    public string? ClosingTime { get; init; }

    /// <summary>Approved doctors only. Pending, rejected, cancelled and removed never appear here.</summary>
    public IReadOnlyList<DirectoryDoctorSummaryDto> Doctors { get; init; } = [];
}

/// <summary>A doctor as shown inside a hospital's details page.</summary>
public class DirectoryDoctorSummaryDto
{
    public Guid DoctorProfileId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public SpecialtyOptionDto? Specialty { get; init; }
    public int? YearsOfExperience { get; init; }
    public decimal? ConsultationPrice { get; init; }
    public string? ProfileImageUrl { get; init; }
}

// ------------------------------------------------------------- Doctor results

public class DoctorDirectoryItemDto
{
    public Guid DoctorProfileId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public SpecialtyOptionDto? Specialty { get; init; }
    public int? YearsOfExperience { get; init; }
    public string? Biography { get; init; }
    public decimal? ConsultationPrice { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public string? ProfileImageUrl { get; init; }

    /// <summary>Approved affiliations only.</summary>
    public IReadOnlyList<DirectoryHospitalSummaryDto> Hospitals { get; init; } = [];
}

public class DoctorDirectoryDetailsDto : DoctorDirectoryItemDto
{
    public string? LicenseNumber { get; init; }
    public string? Address { get; init; }
}

/// <summary>A hospital as shown inside a doctor's details page.</summary>
public class DirectoryHospitalSummaryDto
{
    public Guid Id { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public bool IsPrimary { get; init; }
}
