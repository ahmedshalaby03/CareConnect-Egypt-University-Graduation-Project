using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.BloodBank;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Read-only, cross-hospital blood availability search. Only active hospital accounts with
/// a completed profile are ever surfaced - the same invariant <see cref="HealthcareDirectoryService"/>
/// applies to the doctor/hospital directories.
/// </summary>
public class BloodAvailabilityService : IBloodAvailabilityService
{
    private readonly ApplicationDbContext _context;

    public BloodAvailabilityService(ApplicationDbContext context) => _context = context;

    public async Task<Result<PagedResult<BloodAvailabilityDto>>> SearchAsync(
        BloodAvailabilityQueryParameters query,
        CancellationToken ct = default)
    {
        var stock = _context.BloodStocks
            .AsNoTracking()
            .Where(s => s.HospitalProfile!.IsProfileCompleted && s.HospitalProfile.User!.IsActive);

        if (query.BloodGroup.HasValue)
        {
            stock = stock.Where(s => s.BloodGroup == query.BloodGroup.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Governorate))
        {
            var governorate = query.Governorate.Trim();
            stock = stock.Where(s => s.HospitalProfile!.Governorate == governorate);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            stock = stock.Where(s => s.HospitalProfile!.City == city);
        }

        if (!string.IsNullOrWhiteSpace(query.HospitalName))
        {
            var term = query.HospitalName.Trim();
            stock = stock.Where(s =>
                s.HospitalProfile!.HospitalName != null &&
                EF.Functions.Like(s.HospitalProfile.HospitalName, $"%{term}%"));
        }

        if (query.AvailableOnly == true)
        {
            stock = stock.Where(s => s.IsAvailable);
        }

        var totalCount = await stock.CountAsync(ct);

        var items = await stock
            // Available rows first, then alphabetically by hospital - so a patient scanning
            // the list sees hospitals that can actually help before the ones that cannot.
            .OrderByDescending(s => s.IsAvailable)
            .ThenBy(s => s.HospitalProfile!.HospitalName)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(s => new BloodAvailabilityDto
            {
                HospitalProfileId = s.HospitalProfileId,
                HospitalName = s.HospitalProfile!.HospitalName ?? string.Empty,
                HospitalLogoUrl = s.HospitalProfile.LogoUrl,
                Address = s.HospitalProfile.Address,
                Governorate = s.HospitalProfile.Governorate,
                City = s.HospitalProfile.City,
                PhoneNumber = s.HospitalProfile.PhoneNumber,
                Latitude = s.HospitalProfile.Latitude,
                Longitude = s.HospitalProfile.Longitude,
                BloodGroup = s.BloodGroup,
                BloodGroupDisplayName = s.BloodGroup.ToDisplayName(),
                AvailableUnits = s.AvailableUnits,
                IsAvailable = s.IsAvailable,
                LastUpdatedAt = s.UpdatedAt ?? s.CreatedAt
            })
            .ToListAsync(ct);

        return Result<PagedResult<BloodAvailabilityDto>>.Success(
            PagedResult<BloodAvailabilityDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Blood availability retrieved successfully.");
    }

    public async Task<Result<HospitalBloodBankDetailsDto>> GetHospitalBloodBankAsync(
        Guid hospitalProfileId,
        CancellationToken ct = default)
    {
        var hospital = await _context.HospitalProfiles
            .AsNoTracking()
            .Where(h => h.Id == hospitalProfileId && h.IsProfileCompleted && h.User!.IsActive)
            .Select(h => new
            {
                h.Id,
                HospitalName = h.HospitalName ?? string.Empty,
                h.LogoUrl,
                h.Address,
                h.Governorate,
                h.City,
                h.PhoneNumber,
                h.Latitude,
                h.Longitude,
                BloodGroups = h.BloodStocks
                    .OrderBy(s => s.BloodGroup)
                    .Select(s => new BloodGroupAvailabilityDto
                    {
                        BloodGroup = s.BloodGroup,
                        BloodGroupDisplayName = s.BloodGroup.ToDisplayName(),
                        AvailableUnits = s.AvailableUnits,
                        IsAvailable = s.IsAvailable,
                        LastUpdatedAt = s.UpdatedAt ?? s.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (hospital is null)
        {
            return Result<HospitalBloodBankDetailsDto>.NotFound("Hospital not found.");
        }

        return Result<HospitalBloodBankDetailsDto>.Success(
            new HospitalBloodBankDetailsDto
            {
                HospitalProfileId = hospital.Id,
                HospitalName = hospital.HospitalName,
                HospitalLogoUrl = hospital.LogoUrl,
                Address = hospital.Address,
                Governorate = hospital.Governorate,
                City = hospital.City,
                PhoneNumber = hospital.PhoneNumber,
                Latitude = hospital.Latitude,
                Longitude = hospital.Longitude,
                BloodGroups = hospital.BloodGroups
            },
            "Hospital blood bank retrieved successfully.");
    }
}
