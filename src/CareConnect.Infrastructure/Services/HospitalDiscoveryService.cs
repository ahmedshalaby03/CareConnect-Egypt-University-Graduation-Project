using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Directory;
using CareConnect.Application.DTOs.HospitalDiscovery;
using CareConnect.Application.Interfaces;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Location-aware hospital discovery. Nearby search applies a bounding-box pre-filter in
/// SQL, then an exact Haversine pass and RadiusKm cut in memory - see
/// <see cref="IGeoDistanceService"/>. Nothing here calls an external distance API and
/// nothing here persists the caller's coordinates.
/// </summary>
public class HospitalDiscoveryService : IHospitalDiscoveryService
{
    private readonly ApplicationDbContext _context;
    private readonly IGeoDistanceService _geoDistance;

    public HospitalDiscoveryService(ApplicationDbContext context, IGeoDistanceService geoDistance)
    {
        _context = context;
        _geoDistance = geoDistance;
    }

    public async Task<Result<PagedResult<HospitalDirectoryItemDto>>> SearchNearbyAsync(
        NearbyHospitalQueryParameters query,
        CancellationToken ct = default)
    {
        // The validator guarantees these by this point - guard clauses keep the compiler
        // happy about the non-null decimal values used below.
        if (!query.Latitude.HasValue || !query.Longitude.HasValue)
        {
            return Result<PagedResult<HospitalDirectoryItemDto>>.Invalid(
                "Latitude and longitude are required for a nearby search.");
        }

        var userLatitude = query.Latitude.Value;
        var userLongitude = query.Longitude.Value;
        var box = _geoDistance.CalculateBoundingBox(userLatitude, userLongitude, query.RadiusKm);

        var hospitals = _context.HospitalProfiles
            .AsNoTracking()
            .Where(h => h.IsProfileCompleted
                        && h.User!.IsActive
                        && h.Latitude != null
                        && h.Longitude != null
                        && h.Latitude >= box.MinLatitude && h.Latitude <= box.MaxLatitude
                        && h.Longitude >= box.MinLongitude && h.Longitude <= box.MaxLongitude);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();
            hospitals = hospitals.Where(h =>
                (h.HospitalName != null && EF.Functions.Like(h.HospitalName, $"%{term}%")) ||
                (h.Address != null && EF.Functions.Like(h.Address, $"%{term}%")) ||
                (h.City != null && EF.Functions.Like(h.City, $"%{term}%")) ||
                (h.Governorate != null && EF.Functions.Like(h.Governorate, $"%{term}%")));
        }

        if (!string.IsNullOrWhiteSpace(query.Governorate))
        {
            var governorate = query.Governorate.Trim();
            hospitals = hospitals.Where(h => h.Governorate != null && h.Governorate.ToLower() == governorate.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            hospitals = hospitals.Where(h => h.City != null && h.City.ToLower() == city.ToLower());
        }

        if (query.SpecialtyId.HasValue)
        {
            hospitals = hospitals.Where(h =>
                h.HospitalSpecialties.Any(hs => hs.SpecialtyId == query.SpecialtyId.Value));
        }

        if (query.HasAvailableBlood == true || query.BloodGroup.HasValue)
        {
            hospitals = hospitals.Where(h => h.BloodStocks.Any(s =>
                s.AvailableUnits > 0
                && s.IsAvailable
                && (!query.BloodGroup.HasValue || s.BloodGroup == query.BloodGroup.Value)));
        }

        // Bounding-box candidates only - small enough, for an academic-MVP hospital count,
        // to finish with an exact Haversine pass and the appointment-availability lookup in
        // memory rather than trying to translate either into SQL.
        var candidates = await hospitals
            .Select(HospitalDirectoryProjections.HospitalProjection())
            .ToListAsync(ct);

        var withinRadius = candidates
            .Select(h => (
                Candidate: h,
                DistanceKm: _geoDistance.CalculateDistanceKm(userLatitude, userLongitude, h.Latitude!.Value, h.Longitude!.Value)))
            .Where(x => x.DistanceKm <= query.RadiusKm)
            .ToList();

        var candidateIds = withinRadius.Select(x => x.Candidate.Id).ToList();
        var appointmentAvailability = await HospitalAvailabilityHelpers
            .ComputeAppointmentAvailabilityAsync(_context, candidateIds, ct);

        IEnumerable<(HospitalDirectoryProjections.HospitalCandidate Candidate, double DistanceKm)> filtered = withinRadius;

        if (query.HasAvailableAppointments.HasValue)
        {
            filtered = filtered.Where(x =>
                appointmentAvailability.TryGetValue(x.Candidate.Id, out var a)
                && a.HasAvailableAppointments == query.HasAvailableAppointments.Value);
        }

        var sorted = filtered.OrderBy(x => x.DistanceKm).ToList();
        var totalCount = sorted.Count;

        var items = sorted
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(x => HospitalDirectoryProjections.ToDirectoryItemDto(
                x.Candidate, appointmentAvailability, userLatitude, userLongitude, x.DistanceKm))
            .ToList();

        return Result<PagedResult<HospitalDirectoryItemDto>>.Success(
            PagedResult<HospitalDirectoryItemDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Nearby hospitals retrieved successfully.");
    }

    public async Task<Result<HospitalLocationDetailsDto>> GetLocationDetailsAsync(
        Guid hospitalProfileId,
        decimal? userLatitude,
        decimal? userLongitude,
        CancellationToken ct = default)
    {
        var hospital = await _context.HospitalProfiles
            .AsNoTracking()
            .Where(h => h.Id == hospitalProfileId && h.IsProfileCompleted && h.User!.IsActive)
            .Select(h => new
            {
                h.Id,
                HospitalName = h.HospitalName ?? string.Empty,
                h.Address,
                h.Governorate,
                h.City,
                h.Latitude,
                h.Longitude,
                h.LocationDescription,
                h.NearbyLandmark,
                h.PhoneNumber
            })
            .FirstOrDefaultAsync(ct);

        if (hospital is null)
        {
            return Result<HospitalLocationDetailsDto>.NotFound("Hospital not found.");
        }

        var isLocationCompleted = !string.IsNullOrWhiteSpace(hospital.Address)
            && !string.IsNullOrWhiteSpace(hospital.Governorate)
            && !string.IsNullOrWhiteSpace(hospital.City)
            && hospital.Latitude.HasValue
            && hospital.Longitude.HasValue;

        var distanceKm = userLatitude.HasValue && userLongitude.HasValue
                          && hospital.Latitude.HasValue && hospital.Longitude.HasValue
            ? _geoDistance.CalculateDistanceKm(
                userLatitude.Value, userLongitude.Value, hospital.Latitude.Value, hospital.Longitude.Value)
            : (double?)null;

        return Result<HospitalLocationDetailsDto>.Success(
            new HospitalLocationDetailsDto
            {
                HospitalProfileId = hospital.Id,
                HospitalName = hospital.HospitalName,
                Address = hospital.Address,
                Governorate = hospital.Governorate,
                City = hospital.City,
                Latitude = hospital.Latitude,
                Longitude = hospital.Longitude,
                LocationDescription = hospital.LocationDescription,
                NearbyLandmark = hospital.NearbyLandmark,
                PhoneNumber = hospital.PhoneNumber,
                DirectionsUrl = DirectionsUrlBuilder.Build(hospital.Latitude, hospital.Longitude),
                IsLocationCompleted = isLocationCompleted,
                DistanceKm = distanceKm
            },
            "Hospital location retrieved successfully.");
    }

    public async Task<Result<HospitalLocationOptionsDto>> GetLocationOptionsAsync(CancellationToken ct = default)
    {
        var rows = await _context.HospitalProfiles
            .AsNoTracking()
            .Where(h => h.IsProfileCompleted
                        && h.User!.IsActive
                        && h.Governorate != null
                        && h.City != null)
            .Select(h => new { Governorate = h.Governorate!, City = h.City! })
            .Distinct()
            .ToListAsync(ct);

        var governorates = rows
            .Select(r => r.Governorate)
            .Distinct()
            .OrderBy(g => g, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var citiesByGovernorate = rows
            .GroupBy(r => r.Governorate)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new GovernorateCitiesDto
            {
                Governorate = g.Key,
                Cities = g.Select(r => r.City).Distinct().OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList()
            })
            .ToList();

        return Result<HospitalLocationOptionsDto>.Success(
            new HospitalLocationOptionsDto
            {
                Governorates = governorates,
                CitiesByGovernorate = citiesByGovernorate
            },
            "Location options retrieved successfully.");
    }

    public async Task<Result<SuperAdminHospitalLocationStatsDto>> GetSuperAdminLocationStatsAsync(
        CancellationToken ct = default)
    {
        var eligible = _context.HospitalProfiles
            .AsNoTracking()
            .Where(h => h.IsProfileCompleted && h.User!.IsActive);

        var completedLocationCount = await eligible.CountAsync(
            h => h.Address != null && h.Address != ""
                 && h.Governorate != null && h.Governorate != ""
                 && h.City != null && h.City != ""
                 && h.Latitude != null && h.Longitude != null,
            ct);

        var missingCoordinatesCount = await eligible.CountAsync(
            h => h.Latitude == null || h.Longitude == null, ct);

        var governoratesCovered = (await eligible
                .Where(h => h.Governorate != null && h.Governorate != "")
                .Select(h => h.Governorate!)
                .Distinct()
                .ToListAsync(ct))
            .OrderBy(g => g, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result<SuperAdminHospitalLocationStatsDto>.Success(
            new SuperAdminHospitalLocationStatsDto
            {
                ActiveHospitalsWithCompletedLocationCount = completedLocationCount,
                ActiveHospitalsMissingCoordinatesCount = missingCoordinatesCount,
                GovernoratesCovered = governoratesCovered
            },
            "Location statistics retrieved successfully.");
    }
}
