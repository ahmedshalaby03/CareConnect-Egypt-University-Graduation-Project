using System.Linq.Expressions;
using CareConnect.Application.Common;
using CareConnect.Application.DTOs.Directory;
using CareConnect.Application.DTOs.Specialties;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// The single hospital-search projection shared by <see cref="HealthcareDirectoryService"/>
/// (plain directory, optionally location-aware) and <see cref="HospitalDiscoveryService"/>
/// (nearby search), so the two endpoints can never drift on what a search result looks like.
/// </summary>
internal static class HospitalDirectoryProjections
{
    /// <summary>Everything a search needs for filtering, sorting and display, minus the parts that cannot be computed in SQL.</summary>
    internal sealed record HospitalCandidate(
        Guid Id,
        string HospitalName,
        string? Address,
        string? Governorate,
        string? City,
        string? PhoneNumber,
        string? Description,
        string? LogoUrl,
        decimal? Latitude,
        decimal? Longitude,
        string? LocationDescription,
        string? NearbyLandmark,
        DateTime CreatedAt,
        IReadOnlyList<SpecialtyOptionDto> Specialties,
        int NumberOfApprovedDoctors,
        IReadOnlyList<BloodGroup> AvailableBloodGroups,
        double? AverageRating,
        int ReviewCount);

    private sealed record HospitalCandidateSeed(
        Guid Id,
        string HospitalName,
        string? Address,
        string? Governorate,
        string? City,
        string? PhoneNumber,
        string? Description,
        string? LogoUrl,
        decimal? Latitude,
        decimal? Longitude,
        string? LocationDescription,
        string? NearbyLandmark,
        DateTime CreatedAt,
        int NumberOfApprovedDoctors,
        double? AverageRating,
        int ReviewCount);

    private static Expression<Func<HospitalProfile, HospitalCandidateSeed>> ScalarProjection() =>
        h => new HospitalCandidateSeed(
            h.Id,
            h.HospitalName ?? string.Empty,
            h.Address,
            h.Governorate,
            h.City,
            h.PhoneNumber,
            h.Description,
            h.LogoUrl ?? (h.User!.ProfileImageFileName == null
                ? null
                : ProfileImageStorageConstants.RequestPath + "/" + h.User.ProfileImageFileName),
            h.Latitude,
            h.Longitude,
            h.LocationDescription,
            h.NearbyLandmark,
            h.CreatedAt,
            h.DoctorAffiliations.Count(a => a.Status == AffiliationStatus.Approved),
            h.Reviews.Where(r => r.ModerationStatus == ReviewModerationStatus.Visible)
                .Select(r => (double?)r.Rating).Average(),
            h.Reviews.Count(r => r.ModerationStatus == ReviewModerationStatus.Visible));

    /// <summary>
    /// Loads scalar hospital cards first, then hydrates specialties and blood groups in two
    /// bounded batch queries. This avoids SQL APPLY, remains SQLite-compatible for the
    /// existing test host, and avoids one query per card.
    /// </summary>
    internal static async Task<List<HospitalCandidate>> LoadCandidatesAsync(
        ApplicationDbContext context,
        IQueryable<HospitalProfile> hospitals,
        CancellationToken cancellationToken)
    {
        var seeds = await hospitals.Select(ScalarProjection()).ToListAsync(cancellationToken);
        if (seeds.Count == 0)
        {
            return [];
        }

        var ids = seeds.Select(seed => seed.Id).ToList();
        var specialtyRows = await context.HospitalSpecialties
            .AsNoTracking()
            .Where(link =>
                ids.Contains(link.HospitalProfileId) &&
                link.Specialty!.IsActive)
            .OrderBy(link => link.Specialty!.Name)
            .Select(link => new
            {
                link.HospitalProfileId,
                Specialty = new SpecialtyOptionDto
                {
                    Id = link.SpecialtyId,
                    Name = link.Specialty!.Name,
                    ArabicName = link.Specialty.ArabicName
                }
            })
            .ToListAsync(cancellationToken);

        var bloodRows = await context.BloodStocks
            .AsNoTracking()
            .Where(stock =>
                ids.Contains(stock.HospitalProfileId) &&
                stock.AvailableUnits > 0 &&
                stock.IsAvailable)
            .Select(stock => new { stock.HospitalProfileId, stock.BloodGroup })
            .Distinct()
            .ToListAsync(cancellationToken);

        var specialtiesByHospital = specialtyRows
            .GroupBy(row => row.HospitalProfileId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<SpecialtyOptionDto>)group
                    .Select(row => row.Specialty)
                    .ToList());
        var bloodByHospital = bloodRows
            .GroupBy(row => row.HospitalProfileId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<BloodGroup>)group
                    .Select(row => row.BloodGroup)
                    .ToList());

        return seeds.Select(seed => new HospitalCandidate(
            seed.Id,
            seed.HospitalName,
            seed.Address,
            seed.Governorate,
            seed.City,
            seed.PhoneNumber,
            seed.Description,
            seed.LogoUrl,
            seed.Latitude,
            seed.Longitude,
            seed.LocationDescription,
            seed.NearbyLandmark,
            seed.CreatedAt,
            specialtiesByHospital.GetValueOrDefault(seed.Id) ?? [],
            seed.NumberOfApprovedDoctors,
            bloodByHospital.GetValueOrDefault(seed.Id) ?? [],
            seed.AverageRating,
            seed.ReviewCount)).ToList();
    }

    internal static HospitalDirectoryItemDto ToDirectoryItemDto(
        HospitalCandidate h,
        IReadOnlyDictionary<Guid, HospitalAvailabilityHelpers.AppointmentAvailability> appointmentAvailability,
        decimal? queryLatitude,
        decimal? queryLongitude,
        double? precomputedDistanceKm = null,
        IGeoDistanceService? geoDistance = null)
    {
        var distanceKm = precomputedDistanceKm
            ?? (queryLatitude.HasValue && queryLongitude.HasValue && h.Latitude.HasValue && h.Longitude.HasValue && geoDistance is not null
                ? geoDistance.CalculateDistanceKm(queryLatitude.Value, queryLongitude.Value, h.Latitude.Value, h.Longitude.Value)
                : (double?)null);

        appointmentAvailability.TryGetValue(h.Id, out var availability);

        return new HospitalDirectoryItemDto
        {
            Id = h.Id,
            HospitalName = h.HospitalName,
            Address = h.Address,
            Governorate = h.Governorate,
            City = h.City,
            PhoneNumber = h.PhoneNumber,
            Description = h.Description,
            LogoUrl = h.LogoUrl,
            Latitude = h.Latitude,
            Longitude = h.Longitude,
            LocationDescription = h.LocationDescription,
            NearbyLandmark = h.NearbyLandmark,
            IsLocationCompleted = !string.IsNullOrWhiteSpace(h.Address)
                && !string.IsNullOrWhiteSpace(h.Governorate)
                && !string.IsNullOrWhiteSpace(h.City)
                && h.Latitude.HasValue
                && h.Longitude.HasValue,
            DistanceKm = distanceKm,
            DirectionsUrl = DirectionsUrlBuilder.Build(h.Latitude, h.Longitude),
            Specialties = h.Specialties,
            NumberOfApprovedDoctors = h.NumberOfApprovedDoctors,
            HasAvailableAppointments = availability.HasAvailableAppointments,
            NextAvailableAppointmentAt = availability.NextAvailableAt,
            IsBloodAvailable = h.AvailableBloodGroups.Count > 0,
            AvailableBloodGroups = h.AvailableBloodGroups.Select(bg => bg.ToDisplayName()).ToList(),
            AverageRating = h.AverageRating.HasValue ? Math.Round(h.AverageRating.Value, 1) : null,
            ReviewCount = h.ReviewCount
        };
    }
}
