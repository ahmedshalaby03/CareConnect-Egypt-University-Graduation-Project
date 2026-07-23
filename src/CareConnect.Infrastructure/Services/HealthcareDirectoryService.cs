using System.Globalization;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Directory;
using CareConnect.Application.DTOs.HospitalDiscovery;
using CareConnect.Application.DTOs.Specialties;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Read-only browse endpoints shared by every signed-in role.
///
/// Two invariants hold everywhere in here: only completed profiles on active accounts are
/// listed, and only Approved affiliations are ever surfaced. Pending, rejected, cancelled
/// and removed relationships are invisible to the public.
/// </summary>
public class HealthcareDirectoryService : IHealthcareDirectoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IGeoDistanceService _geoDistance;

    public HealthcareDirectoryService(ApplicationDbContext context, IGeoDistanceService geoDistance)
    {
        _context = context;
        _geoDistance = geoDistance;
    }

    // ------------------------------------------------------------- Hospitals

    /// <summary>
    /// Plain-search filters (name/specialty/location) run in SQL. HasAvailableAppointments
    /// and distance sorting cannot translate to SQL (slot generation is a C# iterator, and
    /// Haversine needs Math.Sin/Cos), so whenever either is in play this loads the full
    /// filtered candidate set - reasonable for an academic-MVP hospital count - and finishes
    /// filtering, sorting and paging in memory. A plain name/location search never pays that
    /// cost: it stays fully paginated in SQL.
    /// </summary>
    public async Task<Result<PagedResult<HospitalDirectoryItemDto>>> SearchHospitalsAsync(
        HospitalDirectoryQueryParameters query,
        CancellationToken ct = default)
    {
        var hospitals = _context.HospitalProfiles
            .AsNoTracking()
            .Where(h => h.IsProfileCompleted && h.User!.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            hospitals = hospitals.Where(h =>
                (h.HospitalName != null && EF.Functions.Like(h.HospitalName, $"%{term}%")) ||
                (h.Description != null && EF.Functions.Like(h.Description, $"%{term}%")) ||
                (h.Address != null && EF.Functions.Like(h.Address, $"%{term}%")) ||
                (h.City != null && EF.Functions.Like(h.City, $"%{term}%")) ||
                (h.Governorate != null && EF.Functions.Like(h.Governorate, $"%{term}%")));
        }

        if (!string.IsNullOrWhiteSpace(query.Governorate))
        {
            var governorate = query.Governorate.Trim();
            hospitals = hospitals.Where(h => h.Governorate == governorate);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            hospitals = hospitals.Where(h => h.City == city);
        }

        if (query.SpecialtyId.HasValue)
        {
            hospitals = hospitals.Where(h =>
                h.HospitalSpecialties.Any(hs => hs.SpecialtyId == query.SpecialtyId.Value));
        }

        if (query.HasLocation == true)
        {
            hospitals = hospitals.Where(h => h.Latitude != null && h.Longitude != null);
        }

        if (query.HasAvailableBlood == true || query.BloodGroup.HasValue)
        {
            hospitals = hospitals.Where(h => h.BloodStocks.Any(s =>
                s.AvailableUnits > 0
                && s.IsAvailable
                && (!query.BloodGroup.HasValue || s.BloodGroup == query.BloodGroup.Value)));
        }

        var needsInMemoryPipeline = query.HasAvailableAppointments.HasValue
            || query.SortBy == HospitalSortBy.Distance;

        if (!needsInMemoryPipeline)
        {
            var totalCount = await hospitals.CountAsync(ct);

            var page = await hospitals
                .OrderBy(h => h.HospitalName)
                .Skip(query.Skip)
                .Take(query.PageSize)
                .Select(HospitalDirectoryProjections.HospitalProjection())
                .ToListAsync(ct);

            var pageIds = page.Select(h => h.Id).ToList();
            var appointmentAvailability = await HospitalAvailabilityHelpers
                .ComputeAppointmentAvailabilityAsync(_context, pageIds, ct);

            var items = page
                .Select(h => HospitalDirectoryProjections.ToDirectoryItemDto(
                    h, appointmentAvailability, query.Latitude, query.Longitude, geoDistance: _geoDistance))
                .ToList();

            return Result<PagedResult<HospitalDirectoryItemDto>>.Success(
                PagedResult<HospitalDirectoryItemDto>.Create(items, query.Page, query.PageSize, totalCount),
                "Hospitals retrieved successfully.");
        }

        var candidates = await hospitals.Select(HospitalDirectoryProjections.HospitalProjection()).ToListAsync(ct);
        var candidateIds = candidates.Select(h => h.Id).ToList();

        var availability = await HospitalAvailabilityHelpers
            .ComputeAppointmentAvailabilityAsync(_context, candidateIds, ct);

        IEnumerable<HospitalDirectoryProjections.HospitalCandidate> filtered = candidates;

        if (query.HasAvailableAppointments.HasValue)
        {
            filtered = filtered.Where(h =>
                availability.TryGetValue(h.Id, out var a) && a.HasAvailableAppointments
                == query.HasAvailableAppointments.Value);
        }

        var withDistance = filtered
            .Select(h => (
                Candidate: h,
                DistanceKm: query.Latitude.HasValue && query.Longitude.HasValue && h.Latitude.HasValue && h.Longitude.HasValue
                    ? _geoDistance.CalculateDistanceKm(query.Latitude.Value, query.Longitude.Value, h.Latitude.Value, h.Longitude.Value)
                    : (double?)null))
            .ToList();

        var sorted = query.SortBy switch
        {
            HospitalSortBy.Distance => withDistance.OrderBy(x => x.DistanceKm ?? double.MaxValue),
            HospitalSortBy.Newest => withDistance.OrderByDescending(x => x.Candidate.CreatedAt),
            HospitalSortBy.City => withDistance.OrderBy(x => x.Candidate.City),
            HospitalSortBy.Governorate => withDistance.OrderBy(x => x.Candidate.Governorate),
            _ => withDistance.OrderBy(x => x.Candidate.HospitalName)
        };

        var sortedList = sorted.ToList();
        var pagedTotal = sortedList.Count;

        var pagedItems = sortedList
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(x => HospitalDirectoryProjections.ToDirectoryItemDto(
                x.Candidate, availability, query.Latitude, query.Longitude, x.DistanceKm))
            .ToList();

        return Result<PagedResult<HospitalDirectoryItemDto>>.Success(
            PagedResult<HospitalDirectoryItemDto>.Create(pagedItems, query.Page, query.PageSize, pagedTotal),
            "Hospitals retrieved successfully.");
    }

    public async Task<Result<HospitalDirectoryDetailsDto>> GetHospitalAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var hospital = await _context.HospitalProfiles
            .AsNoTracking()
            .Where(h => h.Id == id && h.IsProfileCompleted && h.User!.IsActive)
            .Select(h => new
            {
                Profile = h,
                Specialties = h.HospitalSpecialties
                    .OrderBy(hs => hs.Specialty!.Name)
                    .Select(hs => new SpecialtyOptionDto
                    {
                        Id = hs.Specialty!.Id,
                        Name = hs.Specialty.Name,
                        ArabicName = hs.Specialty.ArabicName
                    })
                    .ToList(),

                // Approved only. A rejected or pending doctor must never appear publicly.
                Doctors = h.DoctorAffiliations
                    .Where(a => a.Status == AffiliationStatus.Approved
                                && a.DoctorProfile!.User!.IsActive)
                    .OrderBy(a => a.DoctorProfile!.User!.FullName)
                    .Select(a => new DirectoryDoctorSummaryDto
                    {
                        DoctorProfileId = a.DoctorProfileId,
                        FullName = a.DoctorProfile!.User!.FullName,
                        Specialty = a.DoctorProfile.Specialty == null
                            ? null
                            : new SpecialtyOptionDto
                            {
                                Id = a.DoctorProfile.Specialty.Id,
                                Name = a.DoctorProfile.Specialty.Name,
                                ArabicName = a.DoctorProfile.Specialty.ArabicName
                            },
                        YearsOfExperience = a.DoctorProfile.YearsOfExperience,
                        ConsultationPrice = a.DoctorProfile.ConsultationPrice,
                        ProfileImageUrl = a.DoctorProfile.ProfileImageUrl
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (hospital is null)
        {
            return Result<HospitalDirectoryDetailsDto>.NotFound("Hospital not found.");
        }

        var profile = hospital.Profile;

        return Result<HospitalDirectoryDetailsDto>.Success(
            new HospitalDirectoryDetailsDto
            {
                Id = profile.Id,
                HospitalName = profile.HospitalName ?? string.Empty,
                Address = profile.Address,
                Governorate = profile.Governorate,
                City = profile.City,
                PhoneNumber = profile.PhoneNumber,
                Description = profile.Description,
                LogoUrl = profile.LogoUrl,
                Latitude = profile.Latitude,
                Longitude = profile.Longitude,
                WebsiteUrl = profile.WebsiteUrl,
                OpeningTime = profile.OpeningTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                ClosingTime = profile.ClosingTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                Specialties = hospital.Specialties,
                NumberOfApprovedDoctors = hospital.Doctors.Count,
                Doctors = hospital.Doctors
            },
            "Hospital retrieved successfully.");
    }

    // --------------------------------------------------------------- Doctors

    public async Task<Result<PagedResult<DoctorDirectoryItemDto>>> SearchDoctorsAsync(
        DoctorDirectoryQueryParameters query,
        CancellationToken ct = default)
    {
        var doctors = _context.DoctorProfiles
            .AsNoTracking()
            .Where(d => d.IsProfileCompleted && d.User!.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            doctors = doctors.Where(d =>
                EF.Functions.Like(d.User!.FullName, $"%{term}%") ||
                (d.Biography != null && EF.Functions.Like(d.Biography, $"%{term}%")));
        }

        if (query.SpecialtyId.HasValue)
        {
            doctors = doctors.Where(d => d.SpecialtyId == query.SpecialtyId.Value);
        }

        if (query.HospitalId.HasValue)
        {
            doctors = doctors.Where(d => d.HospitalAffiliations.Any(a =>
                a.HospitalProfileId == query.HospitalId.Value
                && a.Status == AffiliationStatus.Approved));
        }

        if (!string.IsNullOrWhiteSpace(query.Governorate))
        {
            var governorate = query.Governorate.Trim();
            doctors = doctors.Where(d => d.Governorate == governorate);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            doctors = doctors.Where(d => d.City == city);
        }

        var totalCount = await doctors.CountAsync(ct);

        var items = await doctors
            .OrderBy(d => d.User!.FullName)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(d => new DoctorDirectoryItemDto
            {
                DoctorProfileId = d.Id,
                FullName = d.User!.FullName,
                Specialty = d.Specialty == null
                    ? null
                    : new SpecialtyOptionDto
                    {
                        Id = d.Specialty.Id,
                        Name = d.Specialty.Name,
                        ArabicName = d.Specialty.ArabicName
                    },
                YearsOfExperience = d.YearsOfExperience,
                Biography = d.Biography,
                ConsultationPrice = d.ConsultationPrice,
                Governorate = d.Governorate,
                City = d.City,
                ProfileImageUrl = d.ProfileImageUrl,
                Hospitals = d.HospitalAffiliations
                    .Where(a => a.Status == AffiliationStatus.Approved
                                && a.HospitalProfile!.IsProfileCompleted)
                    .OrderByDescending(a => a.IsPrimary)
                    .ThenBy(a => a.HospitalProfile!.HospitalName)
                    .Select(a => new DirectoryHospitalSummaryDto
                    {
                        Id = a.HospitalProfileId,
                        HospitalName = a.HospitalProfile!.HospitalName ?? string.Empty,
                        Governorate = a.HospitalProfile.Governorate,
                        City = a.HospitalProfile.City,
                        IsPrimary = a.IsPrimary
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        return Result<PagedResult<DoctorDirectoryItemDto>>.Success(
            PagedResult<DoctorDirectoryItemDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Doctors retrieved successfully.");
    }

    public async Task<Result<DoctorDirectoryDetailsDto>> GetDoctorAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var doctor = await _context.DoctorProfiles
            .AsNoTracking()
            .Where(d => d.Id == id && d.IsProfileCompleted && d.User!.IsActive)
            .Select(d => new DoctorDirectoryDetailsDto
            {
                DoctorProfileId = d.Id,
                FullName = d.User!.FullName,
                Specialty = d.Specialty == null
                    ? null
                    : new SpecialtyOptionDto
                    {
                        Id = d.Specialty.Id,
                        Name = d.Specialty.Name,
                        ArabicName = d.Specialty.ArabicName
                    },
                YearsOfExperience = d.YearsOfExperience,
                Biography = d.Biography,
                ConsultationPrice = d.ConsultationPrice,
                Governorate = d.Governorate,
                City = d.City,
                ProfileImageUrl = d.ProfileImageUrl,
                LicenseNumber = d.LicenseNumber,
                Address = d.Address,
                Hospitals = d.HospitalAffiliations
                    .Where(a => a.Status == AffiliationStatus.Approved
                                && a.HospitalProfile!.IsProfileCompleted)
                    .OrderByDescending(a => a.IsPrimary)
                    .ThenBy(a => a.HospitalProfile!.HospitalName)
                    .Select(a => new DirectoryHospitalSummaryDto
                    {
                        Id = a.HospitalProfileId,
                        HospitalName = a.HospitalProfile!.HospitalName ?? string.Empty,
                        Governorate = a.HospitalProfile.Governorate,
                        City = a.HospitalProfile.City,
                        IsPrimary = a.IsPrimary
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        return doctor is null
            ? Result<DoctorDirectoryDetailsDto>.NotFound("Doctor not found.")
            : Result<DoctorDirectoryDetailsDto>.Success(doctor, "Doctor retrieved successfully.");
    }
}
