using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Affiliations;
using CareConnect.Application.DTOs.Specialties;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Owns the doctor-hospital relationship for both sides. Every entry point resolves the
/// caller's own profile from their user id first, then checks that the target row belongs
/// to that profile, so swapping an id in the URL never reaches another account's data.
/// </summary>
public class DoctorHospitalAffiliationService : IDoctorHospitalAffiliationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DoctorHospitalAffiliationService> _logger;

    public DoctorHospitalAffiliationService(
        ApplicationDbContext context,
        ILogger<DoctorHospitalAffiliationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =========================================================== Doctor side

    public async Task<Result<PagedResult<DoctorHospitalRequestDto>>> GetDoctorRequestsAsync(
        string doctorUserId,
        DoctorAffiliationQueryParameters query,
        CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<PagedResult<DoctorHospitalRequestDto>>.NotFound(
                "Doctor profile not found for the current account.");
        }

        var affiliations = _context.DoctorHospitalAffiliations
            .AsNoTracking()
            .Where(a => a.DoctorProfileId == doctorProfileId.Value);

        if (query.Status.HasValue)
        {
            affiliations = affiliations.Where(a => a.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.HospitalName))
        {
            var term = query.HospitalName.Trim();
            affiliations = affiliations.Where(a =>
                a.HospitalProfile!.HospitalName != null &&
                EF.Functions.Like(a.HospitalProfile.HospitalName, $"%{term}%"));
        }

        var totalCount = await affiliations.CountAsync(ct);

        var items = await affiliations
            .OrderByDescending(a => a.RequestedAt)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(a => new DoctorHospitalRequestDto
            {
                Id = a.Id,
                HospitalProfileId = a.HospitalProfileId,
                HospitalName = a.HospitalProfile!.HospitalName ?? string.Empty,
                Governorate = a.HospitalProfile.Governorate,
                City = a.HospitalProfile.City,
                Status = a.Status,
                StatusName = a.Status.ToString(),
                RequestedAt = a.RequestedAt,
                ReviewedAt = a.ReviewedAt,
                RejectionReason = a.RejectionReason,
                IsPrimary = a.IsPrimary
            })
            .ToListAsync(ct);

        return Result<PagedResult<DoctorHospitalRequestDto>>.Success(
            PagedResult<DoctorHospitalRequestDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Hospital requests retrieved successfully.");
    }

    public async Task<Result<DoctorHospitalRequestDto>> CreateRequestAsync(
        string doctorUserId,
        CreateAffiliationRequest request,
        CancellationToken ct = default)
    {
        var doctor = await _context.DoctorProfiles
            .Include(d => d.User)
            .Include(d => d.Specialty)
            .FirstOrDefaultAsync(d => d.UserId == doctorUserId, ct);

        if (doctor is null)
        {
            return Result<DoctorHospitalRequestDto>.NotFound(
                "Doctor profile not found for the current account.");
        }

        // Rule 5: a hospital should only ever review complete, reviewable applications.
        if (!doctor.IsProfileCompleted)
        {
            return Result<DoctorHospitalRequestDto>.Invalid(
                "Complete your doctor profile before requesting a hospital affiliation.",
                DoctorProfileService.MissingFieldsFor(doctor, doctor.User?.FullName)
                    .Select(field => $"{field} is required.")
                    .ToList());
        }

        var hospital = await _context.HospitalProfiles
            .Include(h => h.HospitalSpecialties)
            .FirstOrDefaultAsync(h => h.Id == request.HospitalProfileId, ct);

        if (hospital is null)
        {
            return Result<DoctorHospitalRequestDto>.NotFound("Hospital not found.");
        }

        // Rule 6: an incomplete hospital is not open for applications yet.
        if (!hospital.IsProfileCompleted)
        {
            return Result<DoctorHospitalRequestDto>.Invalid(
                "This hospital has not finished setting up its profile and cannot accept requests yet.");
        }

        // Rules 7 and 8: the doctor's specialty has to be one the hospital actually offers.
        if (!hospital.HospitalSpecialties.Any(hs => hs.SpecialtyId == doctor.SpecialtyId))
        {
            var specialtyName = doctor.Specialty?.Name ?? "your specialty";

            return Result<DoctorHospitalRequestDto>.Invalid(
                $"This hospital does not currently list {specialtyName} among its specialties, " +
                "so it cannot accept your request.");
        }

        // Rules 3 and 4: one live relationship per hospital, but a rejected or cancelled
        // request may be resubmitted later.
        var blocking = await _context.DoctorHospitalAffiliations
            .Where(a => a.DoctorProfileId == doctor.Id
                        && a.HospitalProfileId == hospital.Id
                        && (a.Status == AffiliationStatus.Pending || a.Status == AffiliationStatus.Approved))
            .FirstOrDefaultAsync(ct);

        if (blocking is not null)
        {
            return Result<DoctorHospitalRequestDto>.Conflict(
                blocking.Status == AffiliationStatus.Pending
                    ? "You already have a pending request with this hospital."
                    : "You are already affiliated with this hospital.");
        }

        var affiliation = new DoctorHospitalAffiliation
        {
            DoctorProfileId = doctor.Id,
            HospitalProfileId = hospital.Id,
            Status = AffiliationStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.DoctorHospitalAffiliations.Add(affiliation);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Doctor {DoctorProfileId} requested affiliation with hospital {HospitalProfileId}.",
            doctor.Id, hospital.Id);

        return Result<DoctorHospitalRequestDto>.Success(
            ToDoctorDto(affiliation, hospital),
            "Affiliation request sent successfully.");
    }

    public async Task<Result<DoctorHospitalRequestDto>> CancelRequestAsync(
        string doctorUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<DoctorHospitalRequestDto>.NotFound(
                "Doctor profile not found for the current account.");
        }

        var affiliation = await _context.DoctorHospitalAffiliations
            .Include(a => a.HospitalProfile)
            .FirstOrDefaultAsync(a => a.Id == requestId, ct);

        // Same answer whether the row is missing or belongs to another doctor, so this
        // endpoint cannot be used to discover other people's request ids.
        if (affiliation is null || affiliation.DoctorProfileId != doctorProfileId.Value)
        {
            return Result<DoctorHospitalRequestDto>.NotFound("Affiliation request not found.");
        }

        // Rule 11: only a request still awaiting review can be withdrawn.
        if (affiliation.Status != AffiliationStatus.Pending)
        {
            return Result<DoctorHospitalRequestDto>.Invalid(
                $"Only pending requests can be cancelled. This request is {affiliation.Status}.");
        }

        affiliation.Status = AffiliationStatus.Cancelled;
        affiliation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Doctor {DoctorProfileId} cancelled request {RequestId}.",
            doctorProfileId, requestId);

        return Result<DoctorHospitalRequestDto>.Success(
            ToDoctorDto(affiliation, affiliation.HospitalProfile),
            "Request cancelled successfully.");
    }

    public async Task<Result<DoctorHospitalRequestDto>> SetPrimaryHospitalAsync(
        string doctorUserId,
        Guid hospitalProfileId,
        CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<DoctorHospitalRequestDto>.NotFound(
                "Doctor profile not found for the current account.");
        }

        var affiliations = await _context.DoctorHospitalAffiliations
            .Include(a => a.HospitalProfile)
            .Where(a => a.DoctorProfileId == doctorProfileId.Value)
            .ToListAsync(ct);

        var target = affiliations.FirstOrDefault(a =>
            a.HospitalProfileId == hospitalProfileId && a.Status == AffiliationStatus.Approved);

        if (target is null)
        {
            return Result<DoctorHospitalRequestDto>.NotFound(
                "You do not have an approved affiliation with this hospital.");
        }

        // Rules 14 and 15: exactly one primary, so every other flag is cleared first.
        foreach (var affiliation in affiliations.Where(a => a.IsPrimary && a.Id != target.Id))
        {
            affiliation.IsPrimary = false;
            affiliation.UpdatedAt = DateTime.UtcNow;
        }

        target.IsPrimary = true;
        target.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Doctor {DoctorProfileId} set hospital {HospitalProfileId} as primary.",
            doctorProfileId, hospitalProfileId);

        return Result<DoctorHospitalRequestDto>.Success(
            ToDoctorDto(target, target.HospitalProfile),
            "Primary hospital updated successfully.");
    }

    public async Task<Result<IReadOnlyList<DoctorAffiliatedHospitalDto>>> GetDoctorHospitalsAsync(
        string doctorUserId,
        CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<IReadOnlyList<DoctorAffiliatedHospitalDto>>.NotFound(
                "Doctor profile not found for the current account.");
        }

        var hospitals = await _context.DoctorHospitalAffiliations
            .AsNoTracking()
            .Where(a => a.DoctorProfileId == doctorProfileId.Value
                        && a.Status == AffiliationStatus.Approved)
            .OrderByDescending(a => a.IsPrimary)
            .ThenBy(a => a.HospitalProfile!.HospitalName)
            .Select(a => new DoctorAffiliatedHospitalDto
            {
                Id = a.HospitalProfileId,
                HospitalName = a.HospitalProfile!.HospitalName ?? string.Empty,
                Address = a.HospitalProfile.Address,
                Governorate = a.HospitalProfile.Governorate,
                City = a.HospitalProfile.City,
                PhoneNumber = a.HospitalProfile.PhoneNumber,
                Status = a.Status,
                StatusName = a.Status.ToString(),
                IsPrimary = a.IsPrimary
            })
            .ToListAsync(ct);

        return Result<IReadOnlyList<DoctorAffiliatedHospitalDto>>.Success(
            hospitals,
            "Affiliated hospitals retrieved successfully.");
    }

    // ========================================================= Hospital side

    public async Task<Result<PagedResult<HospitalDoctorRequestDto>>> GetHospitalRequestsAsync(
        string hospitalUserId,
        HospitalAffiliationQueryParameters query,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<PagedResult<HospitalDoctorRequestDto>>.NotFound(
                "Hospital profile not found for the current account.");
        }

        var affiliations = FilterHospitalAffiliations(hospitalProfileId.Value, query);

        var totalCount = await affiliations.CountAsync(ct);

        var items = await affiliations
            .OrderByDescending(a => a.RequestedAt)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(a => new HospitalDoctorRequestDto
            {
                Id = a.Id,
                DoctorProfileId = a.DoctorProfileId,
                DoctorName = a.DoctorProfile!.User!.FullName,
                Specialty = a.DoctorProfile.Specialty == null
                    ? null
                    : new SpecialtyOptionDto
                    {
                        Id = a.DoctorProfile.Specialty.Id,
                        Name = a.DoctorProfile.Specialty.Name,
                        ArabicName = a.DoctorProfile.Specialty.ArabicName
                    },
                LicenseNumber = a.DoctorProfile.LicenseNumber,
                YearsOfExperience = a.DoctorProfile.YearsOfExperience,
                Biography = a.DoctorProfile.Biography,
                ProfileImageUrl = a.DoctorProfile.ProfileImageUrl,
                Status = a.Status,
                StatusName = a.Status.ToString(),
                RequestedAt = a.RequestedAt,
                ReviewedAt = a.ReviewedAt,
                RejectionReason = a.RejectionReason,
                IsPrimary = a.IsPrimary
            })
            .ToListAsync(ct);

        return Result<PagedResult<HospitalDoctorRequestDto>>.Success(
            PagedResult<HospitalDoctorRequestDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Doctor requests retrieved successfully.");
    }

    public async Task<Result<HospitalDoctorRequestDto>> ApproveRequestAsync(
        string hospitalUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var (failure, affiliation) = await LoadHospitalAffiliationAsync(hospitalUserId, requestId, ct);
        if (failure is not null)
        {
            return failure;
        }

        if (affiliation!.Status != AffiliationStatus.Pending)
        {
            return Result<HospitalDoctorRequestDto>.Invalid(
                $"Only pending requests can be approved. This request is {affiliation.Status}.");
        }

        affiliation.Status = AffiliationStatus.Approved;
        affiliation.ReviewedAt = DateTime.UtcNow;
        affiliation.ReviewedByUserId = hospitalUserId;
        affiliation.RejectionReason = null;
        affiliation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Hospital {UserId} approved request {RequestId}.", hospitalUserId, requestId);

        return Result<HospitalDoctorRequestDto>.Success(
            ToHospitalDto(affiliation),
            "Doctor approved successfully.");
    }

    public async Task<Result<HospitalDoctorRequestDto>> RejectRequestAsync(
        string hospitalUserId,
        Guid requestId,
        RejectAffiliationRequest request,
        CancellationToken ct = default)
    {
        var (failure, affiliation) = await LoadHospitalAffiliationAsync(hospitalUserId, requestId, ct);
        if (failure is not null)
        {
            return failure;
        }

        if (affiliation!.Status != AffiliationStatus.Pending)
        {
            return Result<HospitalDoctorRequestDto>.Invalid(
                $"Only pending requests can be rejected. This request is {affiliation.Status}.");
        }

        // Rule 10: the reason is mandatory, and the validator has already enforced its shape.
        affiliation.Status = AffiliationStatus.Rejected;
        affiliation.RejectionReason = request.RejectionReason.Trim();
        affiliation.ReviewedAt = DateTime.UtcNow;
        affiliation.ReviewedByUserId = hospitalUserId;
        affiliation.IsPrimary = false;
        affiliation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Hospital {UserId} rejected request {RequestId}.", hospitalUserId, requestId);

        return Result<HospitalDoctorRequestDto>.Success(
            ToHospitalDto(affiliation),
            "Request rejected successfully.");
    }

    public async Task<Result<PagedResult<HospitalDoctorDto>>> GetHospitalDoctorsAsync(
        string hospitalUserId,
        HospitalAffiliationQueryParameters query,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<PagedResult<HospitalDoctorDto>>.NotFound(
                "Hospital profile not found for the current account.");
        }

        // This screen always means "currently working here", so the status filter is fixed.
        var approvedOnly = new HospitalAffiliationQueryParameters
        {
            Status = AffiliationStatus.Approved,
            Search = query.Search,
            SpecialtyId = query.SpecialtyId,
            Page = query.Page,
            PageSize = query.PageSize
        };

        var affiliations = FilterHospitalAffiliations(hospitalProfileId.Value, approvedOnly);

        var totalCount = await affiliations.CountAsync(ct);

        var items = await affiliations
            .OrderBy(a => a.DoctorProfile!.User!.FullName)
            .Skip(approvedOnly.Skip)
            .Take(approvedOnly.PageSize)
            .Select(a => new HospitalDoctorDto
            {
                AffiliationId = a.Id,
                DoctorProfileId = a.DoctorProfileId,
                DoctorName = a.DoctorProfile!.User!.FullName,
                Specialty = a.DoctorProfile.Specialty == null
                    ? null
                    : new SpecialtyOptionDto
                    {
                        Id = a.DoctorProfile.Specialty.Id,
                        Name = a.DoctorProfile.Specialty.Name,
                        ArabicName = a.DoctorProfile.Specialty.ArabicName
                    },
                LicenseNumber = a.DoctorProfile.LicenseNumber,
                YearsOfExperience = a.DoctorProfile.YearsOfExperience,
                ConsultationPrice = a.DoctorProfile.ConsultationPrice,
                ProfileImageUrl = a.DoctorProfile.ProfileImageUrl,
                ApprovedAt = a.ReviewedAt,
                IsPrimary = a.IsPrimary
            })
            .ToListAsync(ct);

        return Result<PagedResult<HospitalDoctorDto>>.Success(
            PagedResult<HospitalDoctorDto>.Create(items, approvedOnly.Page, approvedOnly.PageSize, totalCount),
            "Hospital doctors retrieved successfully.");
    }

    public async Task<Result<HospitalDoctorRequestDto>> RemoveDoctorAsync(
        string hospitalUserId,
        Guid doctorProfileId,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<HospitalDoctorRequestDto>.NotFound(
                "Hospital profile not found for the current account.");
        }

        var affiliation = await _context.DoctorHospitalAffiliations
            .Include(a => a.DoctorProfile).ThenInclude(d => d!.User)
            .Include(a => a.DoctorProfile).ThenInclude(d => d!.Specialty)
            .FirstOrDefaultAsync(
                a => a.DoctorProfileId == doctorProfileId
                     && a.HospitalProfileId == hospitalProfileId.Value
                     && a.Status == AffiliationStatus.Approved,
                ct);

        if (affiliation is null)
        {
            return Result<HospitalDoctorRequestDto>.NotFound(
                "This doctor is not currently affiliated with your hospital.");
        }

        // Rule 13: the row survives as history. Nothing is deleted.
        affiliation.Status = AffiliationStatus.Removed;
        affiliation.ReviewedAt = DateTime.UtcNow;
        affiliation.ReviewedByUserId = hospitalUserId;
        affiliation.IsPrimary = false;
        affiliation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Hospital {UserId} removed doctor {DoctorProfileId}.", hospitalUserId, doctorProfileId);

        return Result<HospitalDoctorRequestDto>.Success(
            ToHospitalDto(affiliation),
            "Doctor removed from the hospital successfully.");
    }

    // =============================================================== Helpers

    private Task<Guid?> GetDoctorProfileIdAsync(string userId, CancellationToken ct) =>
        _context.DoctorProfiles
            .Where(d => d.UserId == userId)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(ct);

    private Task<Guid?> GetHospitalProfileIdAsync(string userId, CancellationToken ct) =>
        _context.HospitalProfiles
            .Where(h => h.UserId == userId)
            .Select(h => (Guid?)h.Id)
            .FirstOrDefaultAsync(ct);

    private IQueryable<DoctorHospitalAffiliation> FilterHospitalAffiliations(
        Guid hospitalProfileId,
        HospitalAffiliationQueryParameters query)
    {
        var affiliations = _context.DoctorHospitalAffiliations
            .AsNoTracking()
            .Where(a => a.HospitalProfileId == hospitalProfileId);

        if (query.Status.HasValue)
        {
            affiliations = affiliations.Where(a => a.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            affiliations = affiliations.Where(a =>
                EF.Functions.Like(a.DoctorProfile!.User!.FullName, $"%{term}%"));
        }

        if (query.SpecialtyId.HasValue)
        {
            affiliations = affiliations.Where(a => a.DoctorProfile!.SpecialtyId == query.SpecialtyId.Value);
        }

        return affiliations;
    }

    /// <summary>
    /// Loads an affiliation and proves it belongs to the calling hospital. Returns the same
    /// "not found" for a missing row and for another hospital's row, so the endpoint cannot
    /// be used to probe for valid request ids.
    /// </summary>
    private async Task<(Result<HospitalDoctorRequestDto>? Failure, DoctorHospitalAffiliation? Affiliation)>
        LoadHospitalAffiliationAsync(string hospitalUserId, Guid requestId, CancellationToken ct)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return (Result<HospitalDoctorRequestDto>.NotFound(
                "Hospital profile not found for the current account."), null);
        }

        var affiliation = await _context.DoctorHospitalAffiliations
            .Include(a => a.DoctorProfile).ThenInclude(d => d!.User)
            .Include(a => a.DoctorProfile).ThenInclude(d => d!.Specialty)
            .FirstOrDefaultAsync(a => a.Id == requestId, ct);

        // Rules 2 and 16: a hospital may only act on requests addressed to itself.
        if (affiliation is null || affiliation.HospitalProfileId != hospitalProfileId.Value)
        {
            return (Result<HospitalDoctorRequestDto>.NotFound("Affiliation request not found."), null);
        }

        return (null, affiliation);
    }

    private static DoctorHospitalRequestDto ToDoctorDto(
        DoctorHospitalAffiliation affiliation,
        HospitalProfile? hospital) => new()
    {
        Id = affiliation.Id,
        HospitalProfileId = affiliation.HospitalProfileId,
        HospitalName = hospital?.HospitalName ?? string.Empty,
        Governorate = hospital?.Governorate,
        City = hospital?.City,
        Status = affiliation.Status,
        StatusName = affiliation.Status.ToString(),
        RequestedAt = affiliation.RequestedAt,
        ReviewedAt = affiliation.ReviewedAt,
        RejectionReason = affiliation.RejectionReason,
        IsPrimary = affiliation.IsPrimary
    };

    private static HospitalDoctorRequestDto ToHospitalDto(DoctorHospitalAffiliation affiliation)
    {
        var doctor = affiliation.DoctorProfile;

        return new HospitalDoctorRequestDto
        {
            Id = affiliation.Id,
            DoctorProfileId = affiliation.DoctorProfileId,
            DoctorName = doctor?.User?.FullName ?? string.Empty,
            Specialty = doctor?.Specialty is null
                ? null
                : new SpecialtyOptionDto
                {
                    Id = doctor.Specialty.Id,
                    Name = doctor.Specialty.Name,
                    ArabicName = doctor.Specialty.ArabicName
                },
            LicenseNumber = doctor?.LicenseNumber,
            YearsOfExperience = doctor?.YearsOfExperience,
            Biography = doctor?.Biography,
            ProfileImageUrl = doctor?.ProfileImageUrl,
            Status = affiliation.Status,
            StatusName = affiliation.Status.ToString(),
            RequestedAt = affiliation.RequestedAt,
            ReviewedAt = affiliation.ReviewedAt,
            RejectionReason = affiliation.RejectionReason,
            IsPrimary = affiliation.IsPrimary
        };
    }
}
