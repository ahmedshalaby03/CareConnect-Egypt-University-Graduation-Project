using CareConnect.Application.Common.Models;
using CareConnect.Domain.Enums;

namespace CareConnect.Application.DTOs.HospitalDiscovery;

/// <summary>Column Angular may sort the directory by. Distance requires Latitude/Longitude.</summary>
public enum HospitalSortBy
{
    Name = 0,
    Newest = 1,
    City = 2,
    Governorate = 3,
    Distance = 4
}

// -------------------------------------------------------------------- Queries

public class NearbyHospitalQueryParameters : PagedQueryParameters
{
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    /// <summary>Validated as 1-200 by <c>NearbyHospitalQueryParametersValidator</c> - out-of-range values are rejected, not clamped.</summary>
    public double RadiusKm { get; set; } = 25;

    public Guid? SpecialtyId { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? SearchTerm { get; set; }
    public bool? HasAvailableAppointments { get; set; }
    public bool? HasAvailableBlood { get; set; }
    public BloodGroup? BloodGroup { get; set; }
}

public class HospitalLocationDetailsQueryParameters
{
    public decimal? UserLatitude { get; set; }
    public decimal? UserLongitude { get; set; }
}

// ----------------------------------------------------------------- Responses

public class HospitalLocationDetailsDto
{
    public Guid HospitalProfileId { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? LocationDescription { get; init; }
    public string? NearbyLandmark { get; init; }
    public string? PhoneNumber { get; init; }
    public string? DirectionsUrl { get; init; }
    public bool IsLocationCompleted { get; init; }
    public double? DistanceKm { get; init; }
}

public class HospitalLocationOptionsDto
{
    public IReadOnlyList<string> Governorates { get; init; } = [];
    public IReadOnlyList<GovernorateCitiesDto> CitiesByGovernorate { get; init; } = [];
}

public class GovernorateCitiesDto
{
    public string Governorate { get; init; } = string.Empty;
    public IReadOnlyList<string> Cities { get; init; } = [];
}

/// <summary>Backs the SuperAdmin dashboard's location-coverage tiles.</summary>
public class SuperAdminHospitalLocationStatsDto
{
    public int ActiveHospitalsWithCompletedLocationCount { get; init; }
    public int ActiveHospitalsMissingCoordinatesCount { get; init; }
    public IReadOnlyList<string> GovernoratesCovered { get; init; } = [];
}
