using System.Globalization;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Scheduling;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// A doctor's recurring weekly schedule. Ownership always comes from the authenticated
/// user id, and every write revalidates the hospital affiliation and the overlap rule
/// against current data rather than trusting what the client already showed.
/// </summary>
public class DoctorAvailabilityService : IDoctorAvailabilityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DoctorAvailabilityService> _logger;

    public DoctorAvailabilityService(ApplicationDbContext context, ILogger<DoctorAvailabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<AvailabilityDto>>> GetOwnAsync(
        string doctorUserId,
        AvailabilityQueryParameters query,
        CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<IReadOnlyList<AvailabilityDto>>.NotFound(
                "Doctor profile not found for the current account.");
        }

        var availabilities = _context.DoctorAvailabilities
            .AsNoTracking()
            .Include(a => a.HospitalProfile)
            .Where(a => a.DoctorProfileId == doctorProfileId.Value);

        if (query.HospitalProfileId.HasValue)
        {
            availabilities = availabilities.Where(a => a.HospitalProfileId == query.HospitalProfileId.Value);
        }

        if (query.DayOfWeek.HasValue)
        {
            availabilities = availabilities.Where(a => a.DayOfWeek == query.DayOfWeek.Value);
        }

        if (query.IsActive.HasValue)
        {
            availabilities = availabilities.Where(a => a.IsActive == query.IsActive.Value);
        }

        var items = await availabilities
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ToListAsync(ct);

        return Result<IReadOnlyList<AvailabilityDto>>.Success(
            items.Select(ToDto).ToList(),
            "Availability retrieved successfully.");
    }

    public async Task<Result<AvailabilityDto>> CreateAsync(
        string doctorUserId,
        CreateAvailabilityRequest request,
        CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<AvailabilityDto>.NotFound("Doctor profile not found for the current account.");
        }

        var startTime = TimeOnly.Parse(request.StartTime, CultureInfo.InvariantCulture);
        var endTime = TimeOnly.Parse(request.EndTime, CultureInfo.InvariantCulture);

        var hospitalCheck = await EnsureApprovedAffiliationAsync(doctorProfileId.Value, request.HospitalProfileId, ct);
        if (hospitalCheck is not null)
        {
            return Result<AvailabilityDto>.Invalid(hospitalCheck);
        }

        var overlap = await FindOverlapAsync(
            doctorProfileId.Value, request.HospitalProfileId, request.DayOfWeek, startTime, endTime, excludingId: null, ct);

        if (overlap is not null)
        {
            return Result<AvailabilityDto>.Conflict(
                $"This overlaps an existing {request.DayOfWeek} schedule from " +
                $"{overlap.StartTime:HH\\:mm} to {overlap.EndTime:HH\\:mm} at the same hospital.");
        }

        var availability = new DoctorAvailability
        {
            DoctorProfileId = doctorProfileId.Value,
            HospitalProfileId = request.HospitalProfileId,
            DayOfWeek = request.DayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            SlotDurationMinutes = request.SlotDurationMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.DoctorAvailabilities.Add(availability);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Doctor {DoctorProfileId} added {Day} availability at hospital {HospitalProfileId}.",
            doctorProfileId, request.DayOfWeek, request.HospitalProfileId);

        return Result<AvailabilityDto>.Success(
            await ToDtoWithHospitalAsync(availability, ct), "Availability created successfully.");
    }

    public async Task<Result<AvailabilityDto>> UpdateAsync(
        string doctorUserId,
        Guid id,
        UpdateAvailabilityRequest request,
        CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<AvailabilityDto>.NotFound("Doctor profile not found for the current account.");
        }

        var availability = await _context.DoctorAvailabilities
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (availability is null || availability.DoctorProfileId != doctorProfileId.Value)
        {
            return Result<AvailabilityDto>.NotFound("Availability record not found.");
        }

        var startTime = TimeOnly.Parse(request.StartTime, CultureInfo.InvariantCulture);
        var endTime = TimeOnly.Parse(request.EndTime, CultureInfo.InvariantCulture);

        var hospitalCheck = await EnsureApprovedAffiliationAsync(doctorProfileId.Value, request.HospitalProfileId, ct);
        if (hospitalCheck is not null)
        {
            return Result<AvailabilityDto>.Invalid(hospitalCheck);
        }

        var overlap = await FindOverlapAsync(
            doctorProfileId.Value, request.HospitalProfileId, request.DayOfWeek, startTime, endTime, id, ct);

        if (overlap is not null)
        {
            return Result<AvailabilityDto>.Conflict(
                $"This overlaps an existing {request.DayOfWeek} schedule from " +
                $"{overlap.StartTime:HH\\:mm} to {overlap.EndTime:HH\\:mm} at the same hospital.");
        }

        availability.HospitalProfileId = request.HospitalProfileId;
        availability.DayOfWeek = request.DayOfWeek;
        availability.StartTime = startTime;
        availability.EndTime = endTime;
        availability.SlotDurationMinutes = request.SlotDurationMinutes;
        availability.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Doctor {DoctorProfileId} updated availability {Id}.", doctorProfileId, id);

        return Result<AvailabilityDto>.Success(
            await ToDtoWithHospitalAsync(availability, ct), "Availability updated successfully.");
    }

    public async Task<Result<AvailabilityDto>> ToggleStatusAsync(
        string doctorUserId,
        Guid id,
        CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<AvailabilityDto>.NotFound("Doctor profile not found for the current account.");
        }

        var availability = await _context.DoctorAvailabilities
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (availability is null || availability.DoctorProfileId != doctorProfileId.Value)
        {
            return Result<AvailabilityDto>.NotFound("Availability record not found.");
        }

        // Reactivating must not silently create an overlap with something added meanwhile.
        if (!availability.IsActive)
        {
            var overlap = await FindOverlapAsync(
                doctorProfileId.Value, availability.HospitalProfileId, availability.DayOfWeek,
                availability.StartTime, availability.EndTime, availability.Id, ct);

            if (overlap is not null)
            {
                return Result<AvailabilityDto>.Conflict(
                    $"Cannot reactivate: it now overlaps another {availability.DayOfWeek} schedule " +
                    $"from {overlap.StartTime:HH\\:mm} to {overlap.EndTime:HH\\:mm}.");
            }
        }

        // Rule 9: deactivating never touches appointments already booked against this block.
        availability.IsActive = !availability.IsActive;
        availability.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Doctor {DoctorProfileId} set IsActive={IsActive} on availability {Id}.",
            doctorProfileId, availability.IsActive, id);

        return Result<AvailabilityDto>.Success(
            await ToDtoWithHospitalAsync(availability, ct),
            availability.IsActive ? "Availability activated successfully." : "Availability deactivated successfully.");
    }

    // ----------------------------------------------------------------- Helpers

    private Task<Guid?> GetDoctorProfileIdAsync(string userId, CancellationToken ct) =>
        _context.DoctorProfiles
            .Where(d => d.UserId == userId)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(ct);

    /// <summary>Rule 2: null when the affiliation is Approved, otherwise the validation message to return.</summary>
    private async Task<string?> EnsureApprovedAffiliationAsync(
        Guid doctorProfileId,
        Guid hospitalProfileId,
        CancellationToken ct)
    {
        var isApproved = await _context.DoctorHospitalAffiliations.AnyAsync(
            a => a.DoctorProfileId == doctorProfileId
                 && a.HospitalProfileId == hospitalProfileId
                 && a.Status == AffiliationStatus.Approved,
            ct);

        return isApproved
            ? null
            : "You can only set availability at a hospital where you have an approved affiliation.";
    }

    /// <summary>Rule 5: the first active block at the same doctor+hospital+day whose time range overlaps.</summary>
    private async Task<DoctorAvailability?> FindOverlapAsync(
        Guid doctorProfileId,
        Guid hospitalProfileId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid? excludingId,
        CancellationToken ct)
    {
        var candidates = _context.DoctorAvailabilities
            .Where(a => a.DoctorProfileId == doctorProfileId
                        && a.HospitalProfileId == hospitalProfileId
                        && a.DayOfWeek == dayOfWeek
                        && a.IsActive);

        if (excludingId.HasValue)
        {
            candidates = candidates.Where(a => a.Id != excludingId.Value);
        }

        return await candidates.FirstOrDefaultAsync(
            a => a.StartTime < endTime && startTime < a.EndTime, ct);
    }

    private async Task<AvailabilityDto> ToDtoWithHospitalAsync(DoctorAvailability availability, CancellationToken ct)
    {
        var hospitalName = await _context.HospitalProfiles
            .Where(h => h.Id == availability.HospitalProfileId)
            .Select(h => h.HospitalName)
            .FirstOrDefaultAsync(ct);

        return ToDto(availability, hospitalName);
    }

    private static AvailabilityDto ToDto(DoctorAvailability availability) =>
        ToDto(availability, availability.HospitalProfile?.HospitalName);

    private static AvailabilityDto ToDto(DoctorAvailability availability, string? hospitalName) => new()
    {
        Id = availability.Id,
        HospitalProfileId = availability.HospitalProfileId,
        HospitalName = hospitalName ?? string.Empty,
        DayOfWeek = availability.DayOfWeek,
        DayOfWeekName = availability.DayOfWeek.ToString(),
        StartTime = availability.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
        EndTime = availability.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
        SlotDurationMinutes = availability.SlotDurationMinutes,
        IsActive = availability.IsActive,
        CreatedAt = availability.CreatedAt,
        UpdatedAt = availability.UpdatedAt
    };
}
