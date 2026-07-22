using System.Globalization;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Directory;
using CareConnect.Application.DTOs.Specialties;
using CareConnect.Application.Interfaces;
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

    public HealthcareDirectoryService(ApplicationDbContext context) => _context = context;

    // ------------------------------------------------------------- Hospitals

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
                (h.Description != null && EF.Functions.Like(h.Description, $"%{term}%")));
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

        var totalCount = await hospitals.CountAsync(ct);

        var items = await hospitals
            .OrderBy(h => h.HospitalName)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(h => new HospitalDirectoryItemDto
            {
                Id = h.Id,
                HospitalName = h.HospitalName ?? string.Empty,
                Address = h.Address,
                Governorate = h.Governorate,
                City = h.City,
                PhoneNumber = h.PhoneNumber,
                Description = h.Description,
                LogoUrl = h.LogoUrl,
                Latitude = h.Latitude,
                Longitude = h.Longitude,
                Specialties = h.HospitalSpecialties
                    .OrderBy(hs => hs.Specialty!.Name)
                    .Select(hs => new SpecialtyOptionDto
                    {
                        Id = hs.Specialty!.Id,
                        Name = hs.Specialty.Name,
                        ArabicName = hs.Specialty.ArabicName
                    })
                    .ToList(),
                NumberOfApprovedDoctors = h.DoctorAffiliations
                    .Count(a => a.Status == AffiliationStatus.Approved)
            })
            .ToListAsync(ct);

        return Result<PagedResult<HospitalDirectoryItemDto>>.Success(
            PagedResult<HospitalDirectoryItemDto>.Create(items, query.Page, query.PageSize, totalCount),
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
