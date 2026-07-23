using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Directory;
using CareConnect.Application.DTOs.HospitalDiscovery;

namespace CareConnect.Application.Interfaces;

/// <summary>
/// Location-aware hospital discovery: nearby search, single-hospital location details, and
/// the governorate/city option list. Read-only and open to any authenticated role, same as
/// <see cref="IHealthcareDirectoryService"/>, which this complements rather than replaces.
/// </summary>
public interface IHospitalDiscoveryService
{
    /// <summary>
    /// Requires valid coordinates. Applies a bounding-box pre-filter, then an exact
    /// Haversine pass in memory, then RadiusKm and pagination - see GeoDistanceService.
    /// </summary>
    Task<Result<PagedResult<HospitalDirectoryItemDto>>> SearchNearbyAsync(
        NearbyHospitalQueryParameters query,
        CancellationToken ct = default);

    /// <summary>
    /// DistanceKm is populated only when both UserLatitude and UserLongitude are supplied.
    /// Returns NotFound for a hospital that is inactive, incomplete, or does not exist.
    /// </summary>
    Task<Result<HospitalLocationDetailsDto>> GetLocationDetailsAsync(
        Guid hospitalProfileId,
        decimal? userLatitude,
        decimal? userLongitude,
        CancellationToken ct = default);

    /// <summary>Distinct Governorate/City values drawn from active, completed hospital profiles.</summary>
    Task<Result<HospitalLocationOptionsDto>> GetLocationOptionsAsync(CancellationToken ct = default);

    /// <summary>Location-coverage counters for the SuperAdmin dashboard.</summary>
    Task<Result<SuperAdminHospitalLocationStatsDto>> GetSuperAdminLocationStatsAsync(
        CancellationToken ct = default);
}
