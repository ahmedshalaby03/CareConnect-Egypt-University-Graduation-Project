using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Scheduling;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

public class DoctorUnavailablePeriodService : IDoctorUnavailablePeriodService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DoctorUnavailablePeriodService> _logger;

    public DoctorUnavailablePeriodService(
        ApplicationDbContext context,
        ILogger<DoctorUnavailablePeriodService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<UnavailablePeriodDto>>> GetOwnAsync(
        string doctorUserId,
        UnavailablePeriodQueryParameters query,
        CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<IReadOnlyList<UnavailablePeriodDto>>.NotFound(
                "Doctor profile not found for the current account.");
        }

        var periods = _context.DoctorUnavailablePeriods
            .AsNoTracking()
            .Include(p => p.HospitalProfile)
            .Where(p => p.DoctorProfileId == doctorProfileId.Value);

        if (query.HospitalProfileId.HasValue)
        {
            periods = periods.Where(p => p.HospitalProfileId == query.HospitalProfileId.Value);
        }

        if (query.DateFrom.HasValue)
        {
            periods = periods.Where(p => p.EndDateTime >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            periods = periods.Where(p => p.StartDateTime <= query.DateTo.Value);
        }

        var items = await periods
            .OrderBy(p => p.StartDateTime)
            .ToListAsync(ct);

        return Result<IReadOnlyList<UnavailablePeriodDto>>.Success(
            items.Select(ToDto).ToList(),
            "Unavailable periods retrieved successfully.");
    }

    public async Task<Result<UnavailablePeriodDto>> CreateAsync(
        string doctorUserId,
        CreateUnavailablePeriodRequest request,
        CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<UnavailablePeriodDto>.NotFound("Doctor profile not found for the current account.");
        }

        var hospitalExists = await _context.HospitalProfiles
            .AnyAsync(h => h.Id == request.HospitalProfileId, ct);

        if (!hospitalExists)
        {
            return Result<UnavailablePeriodDto>.NotFound("Hospital not found.");
        }

        // Rules 5 and 6: an absence must not silently orphan a patient who is already booked.
        var conflicts = await _context.Appointments
            .Where(a => a.DoctorProfileId == doctorProfileId.Value
                        && a.HospitalProfileId == request.HospitalProfileId
                        && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
            .Select(a => new { a.AppointmentDate, a.StartTime, a.EndTime })
            .ToListAsync(ct);

        var overlapping = conflicts
            .Where(a => Overlaps(a.AppointmentDate, a.StartTime, a.EndTime, request.StartDateTime, request.EndDateTime))
            .ToList();

        if (overlapping.Count > 0)
        {
            var sample = overlapping
                .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
                .Take(3)
                .Select(a => $"{a.AppointmentDate:yyyy-MM-dd} {a.StartTime:HH\\:mm}");

            return Result<UnavailablePeriodDto>.Conflict(
                $"You have {overlapping.Count} pending or confirmed appointment(s) in this period " +
                $"({string.Join(", ", sample)}{(overlapping.Count > 3 ? ", ..." : "")}). " +
                "Resolve them before marking this time as unavailable.");
        }

        var period = new DoctorUnavailablePeriod
        {
            DoctorProfileId = doctorProfileId.Value,
            HospitalProfileId = request.HospitalProfileId,
            StartDateTime = DateTime.SpecifyKind(request.StartDateTime, DateTimeKind.Utc),
            EndDateTime = DateTime.SpecifyKind(request.EndDateTime, DateTimeKind.Utc),
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.DoctorUnavailablePeriods.Add(period);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Doctor {DoctorProfileId} added an unavailable period {Start:O} - {End:O}.",
            doctorProfileId, period.StartDateTime, period.EndDateTime);

        var hospitalName = await _context.HospitalProfiles
            .Where(h => h.Id == period.HospitalProfileId)
            .Select(h => h.HospitalName)
            .FirstOrDefaultAsync(ct);

        return Result<UnavailablePeriodDto>.Success(
            ToDto(period, hospitalName), "Unavailable period added successfully.");
    }

    public async Task<Result<bool>> DeleteAsync(string doctorUserId, Guid id, CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<bool>.NotFound("Doctor profile not found for the current account.");
        }

        var period = await _context.DoctorUnavailablePeriods.FirstOrDefaultAsync(p => p.Id == id, ct);

        // Same "not found" whether the row is missing or belongs to another doctor.
        if (period is null || period.DoctorProfileId != doctorProfileId.Value)
        {
            return Result<bool>.NotFound("Unavailable period not found.");
        }

        if (period.StartDateTime <= DateTime.UtcNow)
        {
            return Result<bool>.Invalid("Only a period that has not started yet can be deleted.");
        }

        _context.DoctorUnavailablePeriods.Remove(period);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Doctor {DoctorProfileId} deleted unavailable period {Id}.", doctorProfileId, id);

        return Result<bool>.Success(true, "Unavailable period deleted successfully.");
    }

    // ----------------------------------------------------------------- Helpers

    private Task<Guid?> GetDoctorProfileIdAsync(string userId, CancellationToken ct) =>
        _context.DoctorProfiles
            .Where(d => d.UserId == userId)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(ct);

    private static bool Overlaps(
        DateOnly appointmentDate,
        TimeOnly appointmentStart,
        TimeOnly appointmentEnd,
        DateTime periodStart,
        DateTime periodEnd)
    {
        var start = appointmentDate.ToDateTime(appointmentStart, DateTimeKind.Utc);
        var end = appointmentDate.ToDateTime(appointmentEnd, DateTimeKind.Utc);

        return start < periodEnd && periodStart < end;
    }

    private static UnavailablePeriodDto ToDto(DoctorUnavailablePeriod period) =>
        ToDto(period, period.HospitalProfile?.HospitalName);

    private static UnavailablePeriodDto ToDto(DoctorUnavailablePeriod period, string? hospitalName) => new()
    {
        Id = period.Id,
        HospitalProfileId = period.HospitalProfileId,
        HospitalName = hospitalName ?? string.Empty,
        StartDateTime = period.StartDateTime,
        EndDateTime = period.EndDateTime,
        Reason = period.Reason,
        CreatedAt = period.CreatedAt
    };
}
