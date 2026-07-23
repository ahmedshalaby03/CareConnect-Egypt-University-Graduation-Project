using CareConnect.Application.Common.Models;
using CareConnect.Domain.Enums;

namespace CareConnect.Application.DTOs.BloodBank;

// -------------------------------------------------------------------- Queries

public class BloodAvailabilityQueryParameters : PagedQueryParameters
{
    public BloodGroup? BloodGroup { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? HospitalName { get; set; }
    public bool? AvailableOnly { get; set; }
}

// ----------------------------------------------------------------- Responses

/// <summary>
/// One hospital/blood-group row on the public availability search. Deliberately excludes
/// BloodStock.Notes and LastUpdatedByUserId - internal hospital detail nobody outside the
/// hospital needs to see.
/// </summary>
public class BloodAvailabilityDto
{
    public Guid HospitalProfileId { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string? HospitalLogoUrl { get; init; }
    public string? Address { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public string? PhoneNumber { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }

    public BloodGroup BloodGroup { get; init; }
    public string BloodGroupDisplayName { get; init; } = string.Empty;
    public int AvailableUnits { get; init; }
    public bool IsAvailable { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}

public class HospitalBloodBankDetailsDto
{
    public Guid HospitalProfileId { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string? HospitalLogoUrl { get; init; }
    public string? Address { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public string? PhoneNumber { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }

    public IReadOnlyList<BloodGroupAvailabilityDto> BloodGroups { get; init; } = [];
}

public class BloodGroupAvailabilityDto
{
    public BloodGroup BloodGroup { get; init; }
    public string BloodGroupDisplayName { get; init; } = string.Empty;
    public int AvailableUnits { get; init; }
    public bool IsAvailable { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}
