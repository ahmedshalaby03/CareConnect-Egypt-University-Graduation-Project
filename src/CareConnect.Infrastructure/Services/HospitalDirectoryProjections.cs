using System.Linq.Expressions;
using CareConnect.Application.DTOs.Directory;
using CareConnect.Application.DTOs.Specialties;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;

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
        IReadOnlyList<BloodGroup> AvailableBloodGroups);

    internal static Expression<Func<HospitalProfile, HospitalCandidate>> HospitalProjection() =>
        h => new HospitalCandidate(
            h.Id,
            h.HospitalName ?? string.Empty,
            h.Address,
            h.Governorate,
            h.City,
            h.PhoneNumber,
            h.Description,
            h.LogoUrl,
            h.Latitude,
            h.Longitude,
            h.LocationDescription,
            h.NearbyLandmark,
            h.CreatedAt,
            h.HospitalSpecialties
                .OrderBy(hs => hs.Specialty!.Name)
                .Select(hs => new SpecialtyOptionDto
                {
                    Id = hs.Specialty!.Id,
                    Name = hs.Specialty.Name,
                    ArabicName = hs.Specialty.ArabicName
                })
                .ToList(),
            h.DoctorAffiliations.Count(a => a.Status == AffiliationStatus.Approved),
            h.BloodStocks
                .Where(s => s.AvailableUnits > 0 && s.IsAvailable)
                .Select(s => s.BloodGroup)
                .Distinct()
                .ToList());

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
            AvailableBloodGroups = h.AvailableBloodGroups.Select(bg => bg.ToDisplayName()).ToList()
        };
    }
}
