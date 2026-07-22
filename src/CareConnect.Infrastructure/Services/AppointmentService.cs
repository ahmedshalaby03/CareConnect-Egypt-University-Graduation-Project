using System.Globalization;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Appointments;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Owns the appointment lifecycle for all three sides - Patient, Doctor and Hospital.
/// Ownership always comes from the caller's user id, never from a route or body id, and
/// booking revalidates every rule against fresh data inside a transaction immediately
/// before saving - see <see cref="BookAppointmentAsync"/>.
/// </summary>
public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(ApplicationDbContext context, ILogger<AppointmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =========================================================== Patient side

    public async Task<Result<PagedResult<PatientAppointmentDto>>> GetPatientAppointmentsAsync(
        string patientUserId,
        PatientAppointmentQueryParameters query,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PagedResult<PatientAppointmentDto>>.NotFound(
                "Patient profile not found for the current account.");
        }

        var appointments = _context.Appointments
            .AsNoTracking()
            .Where(a => a.PatientProfileId == patientProfileId.Value);

        appointments = ApplyCommonFilters(appointments, query.Status, query.DateFrom, query.DateTo);

        if (!string.IsNullOrWhiteSpace(query.DoctorName))
        {
            var term = query.DoctorName.Trim();
            appointments = appointments.Where(a => EF.Functions.Like(a.DoctorProfile!.User!.FullName, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.HospitalName))
        {
            var term = query.HospitalName.Trim();
            appointments = appointments.Where(a =>
                a.HospitalProfile!.HospitalName != null &&
                EF.Functions.Like(a.HospitalProfile.HospitalName, $"%{term}%"));
        }

        var totalCount = await appointments.CountAsync(ct);

        // Rule: upcoming appointments nearest-first, previous appointments newest-first.
        // Angular drives which of the two this is via the date filter it sends - a
        // DateFrom in the future (or today) means "what's coming up", so soonest leads.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var ascending = query.DateFrom.HasValue && query.DateFrom.Value >= today;

        appointments = ascending
            ? appointments.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
            : appointments.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.StartTime);

        var items = await appointments
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(PatientProjection())
            .ToListAsync(ct);

        return Result<PagedResult<PatientAppointmentDto>>.Success(
            PagedResult<PatientAppointmentDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Appointments retrieved successfully.");
    }

    public async Task<Result<PatientAppointmentDto>> GetPatientAppointmentByIdAsync(
        string patientUserId,
        Guid appointmentId,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PatientAppointmentDto>.NotFound("Patient profile not found for the current account.");
        }

        // Filtering by the owning patient id up front means a mismatched id and a missing
        // row both come back as the same "not found" - no oracle for probing other
        // patients' appointment ids.
        var appointment = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.Id == appointmentId && a.PatientProfileId == patientProfileId.Value)
            .Select(PatientProjection())
            .FirstOrDefaultAsync(ct);

        return appointment is null
            ? Result<PatientAppointmentDto>.NotFound("Appointment not found.")
            : Result<PatientAppointmentDto>.Success(appointment, "Appointment retrieved successfully.");
    }

    public async Task<Result<PatientAppointmentDto>> BookAppointmentAsync(
        string patientUserId,
        BookAppointmentRequest request,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PatientAppointmentDto>.NotFound("Patient profile not found for the current account.");
        }

        var startTime = TimeOnly.Parse(request.StartTime, CultureInfo.InvariantCulture);

        var doctor = await _context.DoctorProfiles
            .AsNoTracking()
            .Include(d => d.User)
            .Include(d => d.Specialty)
            .FirstOrDefaultAsync(d => d.Id == request.DoctorProfileId, ct);

        if (doctor is null)
        {
            return Result<PatientAppointmentDto>.NotFound("Doctor not found.");
        }

        // Rules 4 and 5.
        if (doctor.User?.IsActive != true)
        {
            return Result<PatientAppointmentDto>.Invalid("This doctor's account is not currently active.");
        }

        if (!doctor.IsProfileCompleted)
        {
            return Result<PatientAppointmentDto>.Invalid("This doctor has not finished setting up their profile.");
        }

        var hospital = await _context.HospitalProfiles
            .AsNoTracking()
            .Include(h => h.User)
            .Include(h => h.HospitalSpecialties)
            .FirstOrDefaultAsync(h => h.Id == request.HospitalProfileId, ct);

        if (hospital is null)
        {
            return Result<PatientAppointmentDto>.NotFound("Hospital not found.");
        }

        // Rules 6 and 7.
        if (hospital.User?.IsActive != true)
        {
            return Result<PatientAppointmentDto>.Invalid("This hospital's account is not currently active.");
        }

        if (!hospital.IsProfileCompleted)
        {
            return Result<PatientAppointmentDto>.Invalid("This hospital has not finished setting up its profile.");
        }

        // Rule 8.
        var isApproved = await _context.DoctorHospitalAffiliations.AnyAsync(
            a => a.DoctorProfileId == doctor.Id
                 && a.HospitalProfileId == hospital.Id
                 && a.Status == AffiliationStatus.Approved,
            ct);

        if (!isApproved)
        {
            return Result<PatientAppointmentDto>.Invalid(
                "This doctor is not currently affiliated with the selected hospital.");
        }

        // Rule 9.
        if (!hospital.HospitalSpecialties.Any(hs => hs.SpecialtyId == doctor.SpecialtyId))
        {
            return Result<PatientAppointmentDto>.Invalid(
                "This hospital does not offer the doctor's specialty.");
        }

        // Rules 10 and 11.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.AppointmentDate < today)
        {
            return Result<PatientAppointmentDto>.Invalid("The selected date is in the past.");
        }

        var requestedStart = request.AppointmentDate.ToDateTime(startTime, DateTimeKind.Utc);
        if (requestedStart <= DateTime.UtcNow)
        {
            return Result<PatientAppointmentDto>.Invalid("The selected time has already passed.");
        }

        // Rule 12: the slot must belong exactly to a generated slot from an active block.
        var block = await _context.DoctorAvailabilities
            .AsNoTracking()
            .Where(a => a.DoctorProfileId == doctor.Id
                        && a.HospitalProfileId == hospital.Id
                        && a.DayOfWeek == request.AppointmentDate.DayOfWeek
                        && a.IsActive)
            .ToListAsync(ct);

        var matchingBlock = block.FirstOrDefault(b => b.GenerateSlotStarts().Contains(startTime));
        if (matchingBlock is null)
        {
            return Result<PatientAppointmentDto>.Invalid(
                "The selected time is not a valid appointment slot for this doctor.");
        }

        var endTime = startTime.AddMinutes(matchingBlock.SlotDurationMinutes);
        var requestedEnd = request.AppointmentDate.ToDateTime(endTime, DateTimeKind.Utc);

        // Rule 13.
        var overlapsUnavailablePeriod = await _context.DoctorUnavailablePeriods
            .AsNoTracking()
            .AnyAsync(p => p.DoctorProfileId == doctor.Id
                           && p.HospitalProfileId == hospital.Id
                           && p.StartDateTime < requestedEnd
                           && requestedStart < p.EndDateTime,
                ct);

        if (overlapsUnavailablePeriod)
        {
            return Result<PatientAppointmentDto>.Conflict(
                "The doctor is not available at the selected time.");
        }

        // Rules 14, 15 and 16: only Pending and Confirmed appointments hold a slot. A
        // doctor is one person, so this checks every hospital, not just the selected one.
        var doctorBusy = await _context.Appointments
            .AsNoTracking()
            .AnyAsync(a => a.DoctorProfileId == doctor.Id
                           && a.AppointmentDate == request.AppointmentDate
                           && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
                           && a.StartTime < endTime && startTime < a.EndTime,
                ct);

        if (doctorBusy)
        {
            return Result<PatientAppointmentDto>.Conflict(
                "This slot was just booked by another patient. Please choose a different time.");
        }

        // Rule 17.
        var patientBusy = await _context.Appointments
            .AsNoTracking()
            .AnyAsync(a => a.PatientProfileId == patientProfileId.Value
                           && a.AppointmentDate == request.AppointmentDate
                           && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
                           && a.StartTime < endTime && startTime < a.EndTime,
                ct);

        if (patientBusy)
        {
            return Result<PatientAppointmentDto>.Conflict(
                "You already have another appointment that overlaps this time.");
        }

        var appointment = new Appointment
        {
            PatientProfileId = patientProfileId.Value,
            DoctorProfileId = doctor.Id,
            HospitalProfileId = hospital.Id,
            AppointmentDate = request.AppointmentDate,
            StartTime = startTime,
            EndTime = endTime,
            Reason = request.Reason.Trim(),
            PatientNotes = string.IsNullOrWhiteSpace(request.PatientNotes) ? null : request.PatientNotes.Trim(),
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);

        // Rule 20 and section 15: the transaction plus a final overlap re-check right
        // before save closes the race between two patients booking the same slot at once.
        // The filtered unique index on (DoctorProfileId, AppointmentDate, StartTime) for
        // Pending/Confirmed rows is the last line of defence if both requests still reach
        // SaveChanges together.
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var stillFree = !await _context.Appointments
                .AsNoTracking()
                .AnyAsync(a => a.Id != appointment.Id
                               && a.DoctorProfileId == doctor.Id
                               && a.AppointmentDate == request.AppointmentDate
                               && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
                               && a.StartTime < endTime && startTime < a.EndTime,
                    ct);

            if (!stillFree)
            {
                return Result<PatientAppointmentDto>.Conflict(
                    "This slot was just booked by another patient. Please choose a different time.");
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "Booking race detected for doctor {DoctorProfileId} at {Start:O}.",
                doctor.Id, requestedStart);

            return Result<PatientAppointmentDto>.Conflict(
                "This slot was just booked by another patient. Please choose a different time.");
        }

        _logger.LogInformation(
            "Patient {PatientProfileId} booked appointment {AppointmentId} with doctor {DoctorProfileId}.",
            patientProfileId, appointment.Id, doctor.Id);

        var dto = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.Id == appointment.Id)
            .Select(PatientProjection())
            .FirstAsync(ct);

        return Result<PatientAppointmentDto>.Success(dto, "Appointment requested successfully.");
    }

    public async Task<Result<PatientAppointmentDto>> CancelByPatientAsync(
        string patientUserId,
        Guid appointmentId,
        CancelAppointmentRequest request,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PatientAppointmentDto>.NotFound("Patient profile not found for the current account.");
        }

        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

        if (appointment is null || appointment.PatientProfileId != patientProfileId.Value)
        {
            return Result<PatientAppointmentDto>.NotFound("Appointment not found.");
        }

        var invalid = ValidateCancellable(appointment.Status);
        if (invalid is not null)
        {
            return Result<PatientAppointmentDto>.Invalid(invalid);
        }

        ApplyCancellation(appointment, patientUserId, request.CancellationReason);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Patient {PatientProfileId} cancelled appointment {AppointmentId}.",
            patientProfileId, appointmentId);

        var dto = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(PatientProjection())
            .FirstAsync(ct);

        return Result<PatientAppointmentDto>.Success(dto, "Appointment cancelled successfully.");
    }

    public async Task<Result<PatientDashboardStatsDto>> GetPatientDashboardStatsAsync(
        string patientUserId,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PatientDashboardStatsDto>.NotFound("Patient profile not found for the current account.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);

        var upcoming = _context.Appointments
            .AsNoTracking()
            .Where(a => a.PatientProfileId == patientProfileId.Value
                        && a.Status == AppointmentStatus.Confirmed
                        && (a.AppointmentDate > today || (a.AppointmentDate == today && a.StartTime >= now)));

        var nextAppointment = await upcoming
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
            .Select(PatientProjection())
            .FirstOrDefaultAsync(ct);

        var upcomingCount = await upcoming.CountAsync(ct);

        var pendingCount = await _context.Appointments
            .AsNoTracking()
            .CountAsync(a => a.PatientProfileId == patientProfileId.Value
                              && a.Status == AppointmentStatus.Pending,
                ct);

        return Result<PatientDashboardStatsDto>.Success(
            new PatientDashboardStatsDto
            {
                NextAppointment = nextAppointment,
                UpcomingCount = upcomingCount,
                PendingCount = pendingCount
            },
            "Dashboard statistics retrieved successfully.");
    }

    // ========================================================== Doctor side

    public async Task<Result<PagedResult<DoctorAppointmentDto>>> GetDoctorAppointmentsAsync(
        string doctorUserId,
        DoctorAppointmentQueryParameters query,
        CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<PagedResult<DoctorAppointmentDto>>.NotFound(
                "Doctor profile not found for the current account.");
        }

        var appointments = _context.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorProfileId == doctorProfileId.Value);

        appointments = ApplyCommonFilters(appointments, query.Status, query.DateFrom, query.DateTo);

        if (query.Date.HasValue)
        {
            appointments = appointments.Where(a => a.AppointmentDate == query.Date.Value);
        }

        if (query.HospitalProfileId.HasValue)
        {
            appointments = appointments.Where(a => a.HospitalProfileId == query.HospitalProfileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.PatientName))
        {
            var term = query.PatientName.Trim();
            appointments = appointments.Where(a => EF.Functions.Like(a.PatientProfile!.User!.FullName, $"%{term}%"));
        }

        var totalCount = await appointments.CountAsync(ct);

        var items = await appointments
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(DoctorProjection())
            .ToListAsync(ct);

        return Result<PagedResult<DoctorAppointmentDto>>.Success(
            PagedResult<DoctorAppointmentDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Appointments retrieved successfully.");
    }

    public async Task<Result<DoctorAppointmentDto>> GetDoctorAppointmentByIdAsync(
        string doctorUserId,
        Guid appointmentId,
        CancellationToken ct = default)
    {
        var (failure, appointment) = await LoadDoctorAppointmentAsync(doctorUserId, appointmentId, ct);
        if (failure is not null)
        {
            return failure;
        }

        var dto = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(DoctorProjection())
            .FirstAsync(ct);

        return Result<DoctorAppointmentDto>.Success(dto, "Appointment retrieved successfully.");
    }

    public async Task<Result<DoctorAppointmentDto>> ConfirmAsync(
        string doctorUserId,
        Guid appointmentId,
        CancellationToken ct = default)
    {
        var (failure, appointment) = await LoadDoctorAppointmentAsync(doctorUserId, appointmentId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        if (appointment!.Status != AppointmentStatus.Pending)
        {
            return Result<DoctorAppointmentDto>.Invalid(
                $"Only pending appointments can be confirmed. This appointment is {appointment.Status}.");
        }

        appointment.Status = AppointmentStatus.Confirmed;
        appointment.ConfirmedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<DoctorAppointmentDto>.Success(
            await ReloadDoctorDtoAsync(appointmentId, ct), "Appointment confirmed successfully.");
    }

    public async Task<Result<DoctorAppointmentDto>> RejectAsync(
        string doctorUserId,
        Guid appointmentId,
        RejectAppointmentRequest request,
        CancellationToken ct = default)
    {
        var (failure, appointment) = await LoadDoctorAppointmentAsync(doctorUserId, appointmentId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        if (appointment!.Status != AppointmentStatus.Pending)
        {
            return Result<DoctorAppointmentDto>.Invalid(
                $"Only pending appointments can be rejected. This appointment is {appointment.Status}.");
        }

        appointment.Status = AppointmentStatus.Rejected;
        appointment.RejectionReason = request.RejectionReason.Trim();
        appointment.RejectedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<DoctorAppointmentDto>.Success(
            await ReloadDoctorDtoAsync(appointmentId, ct), "Appointment rejected successfully.");
    }

    public async Task<Result<DoctorAppointmentDto>> CancelByDoctorAsync(
        string doctorUserId,
        Guid appointmentId,
        CancelAppointmentRequest request,
        CancellationToken ct = default)
    {
        var (failure, appointment) = await LoadDoctorAppointmentAsync(doctorUserId, appointmentId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        var invalid = ValidateCancellable(appointment!.Status);
        if (invalid is not null)
        {
            return Result<DoctorAppointmentDto>.Invalid(invalid);
        }

        ApplyCancellation(appointment, doctorUserId, request.CancellationReason);
        await _context.SaveChangesAsync(ct);

        return Result<DoctorAppointmentDto>.Success(
            await ReloadDoctorDtoAsync(appointmentId, ct), "Appointment cancelled successfully.");
    }

    public async Task<Result<DoctorAppointmentDto>> CompleteAsync(
        string doctorUserId,
        Guid appointmentId,
        CancellationToken ct = default)
    {
        var (failure, appointment) = await LoadDoctorAppointmentAsync(doctorUserId, appointmentId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        if (appointment!.Status != AppointmentStatus.Confirmed)
        {
            return Result<DoctorAppointmentDto>.Invalid(
                $"Only confirmed appointments can be completed. This appointment is {appointment.Status}.");
        }

        appointment.Status = AppointmentStatus.Completed;
        appointment.CompletedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<DoctorAppointmentDto>.Success(
            await ReloadDoctorDtoAsync(appointmentId, ct), "Appointment marked as completed.");
    }

    public async Task<Result<DoctorAppointmentDto>> MarkNoShowAsync(
        string doctorUserId,
        Guid appointmentId,
        CancellationToken ct = default)
    {
        var (failure, appointment) = await LoadDoctorAppointmentAsync(doctorUserId, appointmentId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        if (appointment!.Status != AppointmentStatus.Confirmed)
        {
            return Result<DoctorAppointmentDto>.Invalid(
                $"Only confirmed appointments can be marked as a no-show. This appointment is {appointment.Status}.");
        }

        appointment.Status = AppointmentStatus.NoShow;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<DoctorAppointmentDto>.Success(
            await ReloadDoctorDtoAsync(appointmentId, ct), "Appointment marked as a no-show.");
    }

    public async Task<Result<DoctorAppointmentDto>> UpdateNotesAsync(
        string doctorUserId,
        Guid appointmentId,
        DoctorNotesRequest request,
        CancellationToken ct = default)
    {
        var (failure, appointment) = await LoadDoctorAppointmentAsync(doctorUserId, appointmentId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        appointment!.DoctorNotes = string.IsNullOrWhiteSpace(request.DoctorNotes)
            ? null
            : request.DoctorNotes.Trim();
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<DoctorAppointmentDto>.Success(
            await ReloadDoctorDtoAsync(appointmentId, ct), "Notes saved successfully.");
    }

    public async Task<Result<DoctorDashboardStatsDto>> GetDoctorDashboardStatsAsync(
        string doctorUserId,
        CancellationToken ct = default)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return Result<DoctorDashboardStatsDto>.NotFound("Doctor profile not found for the current account.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var todayCount = await _context.Appointments.AsNoTracking().CountAsync(
            a => a.DoctorProfileId == doctorProfileId.Value && a.AppointmentDate == today, ct);

        var pendingCount = await _context.Appointments.AsNoTracking().CountAsync(
            a => a.DoctorProfileId == doctorProfileId.Value && a.Status == AppointmentStatus.Pending, ct);

        var confirmedCount = await _context.Appointments.AsNoTracking().CountAsync(
            a => a.DoctorProfileId == doctorProfileId.Value && a.Status == AppointmentStatus.Confirmed, ct);

        var completedThisMonth = await _context.Appointments.AsNoTracking().CountAsync(
            a => a.DoctorProfileId == doctorProfileId.Value
                 && a.Status == AppointmentStatus.Completed
                 && a.AppointmentDate >= monthStart,
            ct);

        return Result<DoctorDashboardStatsDto>.Success(
            new DoctorDashboardStatsDto
            {
                TodayCount = todayCount,
                PendingCount = pendingCount,
                ConfirmedCount = confirmedCount,
                CompletedThisMonthCount = completedThisMonth
            },
            "Dashboard statistics retrieved successfully.");
    }

    // ======================================================== Hospital side

    public async Task<Result<PagedResult<HospitalAppointmentDto>>> GetHospitalAppointmentsAsync(
        string hospitalUserId,
        HospitalAppointmentQueryParameters query,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<PagedResult<HospitalAppointmentDto>>.NotFound(
                "Hospital profile not found for the current account.");
        }

        var appointments = _context.Appointments
            .AsNoTracking()
            .Where(a => a.HospitalProfileId == hospitalProfileId.Value);

        appointments = ApplyCommonFilters(appointments, query.Status, query.DateFrom, query.DateTo);

        if (query.Date.HasValue)
        {
            appointments = appointments.Where(a => a.AppointmentDate == query.Date.Value);
        }

        if (query.DoctorProfileId.HasValue)
        {
            appointments = appointments.Where(a => a.DoctorProfileId == query.DoctorProfileId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.DoctorName))
        {
            var term = query.DoctorName.Trim();
            appointments = appointments.Where(a => EF.Functions.Like(a.DoctorProfile!.User!.FullName, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.PatientName))
        {
            var term = query.PatientName.Trim();
            appointments = appointments.Where(a => EF.Functions.Like(a.PatientProfile!.User!.FullName, $"%{term}%"));
        }

        var totalCount = await appointments.CountAsync(ct);

        var items = await appointments
            .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(HospitalProjection())
            .ToListAsync(ct);

        return Result<PagedResult<HospitalAppointmentDto>>.Success(
            PagedResult<HospitalAppointmentDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Appointments retrieved successfully.");
    }

    public async Task<Result<HospitalAppointmentDto>> GetHospitalAppointmentByIdAsync(
        string hospitalUserId,
        Guid appointmentId,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<HospitalAppointmentDto>.NotFound("Hospital profile not found for the current account.");
        }

        var owningHospitalId = await _context.Appointments
            .Where(a => a.Id == appointmentId)
            .Select(a => (Guid?)a.HospitalProfileId)
            .FirstOrDefaultAsync(ct);

        if (owningHospitalId is null || owningHospitalId != hospitalProfileId.Value)
        {
            return Result<HospitalAppointmentDto>.NotFound("Appointment not found.");
        }

        var dto = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(HospitalProjection())
            .FirstAsync(ct);

        return Result<HospitalAppointmentDto>.Success(dto, "Appointment retrieved successfully.");
    }

    public async Task<Result<HospitalDashboardStatsDto>> GetHospitalDashboardStatsAsync(
        string hospitalUserId,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<HospitalDashboardStatsDto>.NotFound("Hospital profile not found for the current account.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var todayCount = await _context.Appointments.AsNoTracking().CountAsync(
            a => a.HospitalProfileId == hospitalProfileId.Value && a.AppointmentDate == today, ct);

        var pendingCount = await _context.Appointments.AsNoTracking().CountAsync(
            a => a.HospitalProfileId == hospitalProfileId.Value && a.Status == AppointmentStatus.Pending, ct);

        var activeApprovedDoctors = await _context.DoctorHospitalAffiliations.AsNoTracking().CountAsync(
            a => a.HospitalProfileId == hospitalProfileId.Value
                 && a.Status == AffiliationStatus.Approved
                 && a.DoctorProfile!.User!.IsActive,
            ct);

        return Result<HospitalDashboardStatsDto>.Success(
            new HospitalDashboardStatsDto
            {
                TodayCount = todayCount,
                PendingCount = pendingCount,
                ActiveApprovedDoctorsCount = activeApprovedDoctors
            },
            "Dashboard statistics retrieved successfully.");
    }

    // =============================================================== Helpers

    private Task<Guid?> GetPatientProfileIdAsync(string userId, CancellationToken ct) =>
        _context.PatientProfiles
            .Where(p => p.UserId == userId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);

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

    private static IQueryable<Appointment> ApplyCommonFilters(
        IQueryable<Appointment> appointments,
        AppointmentStatus? status,
        DateOnly? dateFrom,
        DateOnly? dateTo)
    {
        if (status.HasValue)
        {
            appointments = appointments.Where(a => a.Status == status.Value);
        }

        if (dateFrom.HasValue)
        {
            appointments = appointments.Where(a => a.AppointmentDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            appointments = appointments.Where(a => a.AppointmentDate <= dateTo.Value);
        }

        return appointments;
    }

    /// <summary>Rules: only Pending or Confirmed may be cancelled; Completed never can.</summary>
    private static string? ValidateCancellable(AppointmentStatus status) =>
        status is AppointmentStatus.Pending or AppointmentStatus.Confirmed
            ? null
            : $"Only pending or confirmed appointments can be cancelled. This appointment is {status}.";

    private static void ApplyCancellation(Appointment appointment, string cancelledByUserId, string reason)
    {
        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancellationReason = reason.Trim();
        appointment.CancelledByUserId = cancelledByUserId;
        appointment.CancelledAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Loads an appointment and proves it belongs to the calling doctor. Returns the same
    /// "not found" for a missing row and for another doctor's row.
    /// </summary>
    private async Task<(Result<DoctorAppointmentDto>? Failure, Appointment? Appointment)> LoadDoctorAppointmentAsync(
        string doctorUserId,
        Guid appointmentId,
        CancellationToken ct,
        bool tracking = false)
    {
        var doctorProfileId = await GetDoctorProfileIdAsync(doctorUserId, ct);
        if (doctorProfileId is null)
        {
            return (Result<DoctorAppointmentDto>.NotFound("Doctor profile not found for the current account."), null);
        }

        var query = _context.Appointments.Where(a => a.Id == appointmentId);
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        var appointment = await query.FirstOrDefaultAsync(ct);

        if (appointment is null || appointment.DoctorProfileId != doctorProfileId.Value)
        {
            return (Result<DoctorAppointmentDto>.NotFound("Appointment not found."), null);
        }

        return (null, appointment);
    }

    private async Task<DoctorAppointmentDto> ReloadDoctorDtoAsync(Guid appointmentId, CancellationToken ct) =>
        await _context.Appointments
            .AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(DoctorProjection())
            .FirstAsync(ct);

    private static System.Linq.Expressions.Expression<Func<Appointment, PatientAppointmentDto>> PatientProjection() =>
        a => new PatientAppointmentDto
        {
            AppointmentId = a.Id,
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime.ToString("HH:mm:ss"),
            EndTime = a.EndTime.ToString("HH:mm:ss"),
            Status = a.Status,
            StatusName = a.Status.ToString(),
            Reason = a.Reason,
            PatientNotes = a.PatientNotes,
            DoctorProfileId = a.DoctorProfileId,
            DoctorName = a.DoctorProfile!.User!.FullName,
            DoctorSpecialty = a.DoctorProfile.Specialty != null ? a.DoctorProfile.Specialty.Name : null,
            HospitalProfileId = a.HospitalProfileId,
            HospitalName = a.HospitalProfile!.HospitalName ?? string.Empty,
            HospitalAddress = a.HospitalProfile.Address,
            RejectionReason = a.RejectionReason,
            CancellationReason = a.CancellationReason,
            CreatedAt = a.CreatedAt
        };

    private static System.Linq.Expressions.Expression<Func<Appointment, DoctorAppointmentDto>> DoctorProjection() =>
        a => new DoctorAppointmentDto
        {
            AppointmentId = a.Id,
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime.ToString("HH:mm:ss"),
            EndTime = a.EndTime.ToString("HH:mm:ss"),
            Status = a.Status,
            StatusName = a.Status.ToString(),
            Reason = a.Reason,
            PatientNotes = a.PatientNotes,
            DoctorNotes = a.DoctorNotes,
            PatientProfileId = a.PatientProfileId,
            PatientName = a.PatientProfile!.User!.FullName,
            PatientPhoneNumber = a.PatientProfile.User!.PhoneNumber,
            HospitalProfileId = a.HospitalProfileId,
            HospitalName = a.HospitalProfile!.HospitalName ?? string.Empty,
            RejectionReason = a.RejectionReason,
            CancellationReason = a.CancellationReason,
            ConfirmedAt = a.ConfirmedAt,
            RejectedAt = a.RejectedAt,
            CancelledAt = a.CancelledAt,
            CompletedAt = a.CompletedAt,
            CreatedAt = a.CreatedAt
        };

    private static System.Linq.Expressions.Expression<Func<Appointment, HospitalAppointmentDto>> HospitalProjection() =>
        a => new HospitalAppointmentDto
        {
            AppointmentId = a.Id,
            AppointmentDate = a.AppointmentDate,
            StartTime = a.StartTime.ToString("HH:mm:ss"),
            EndTime = a.EndTime.ToString("HH:mm:ss"),
            Status = a.Status,
            StatusName = a.Status.ToString(),
            Reason = a.Reason,
            DoctorProfileId = a.DoctorProfileId,
            DoctorName = a.DoctorProfile!.User!.FullName,
            DoctorSpecialty = a.DoctorProfile.Specialty != null ? a.DoctorProfile.Specialty.Name : null,
            PatientName = a.PatientProfile!.User!.FullName,
            CreatedAt = a.CreatedAt
        };
}
