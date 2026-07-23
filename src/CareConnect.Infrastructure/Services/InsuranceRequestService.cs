using System.Linq.Expressions;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.InsuranceRequests;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Owns the digital insurance request lifecycle for both sides - Patient and Hospital.
/// Ownership always comes from the caller's user id, never from a route or body id, and
/// submission revalidates every eligibility and duplicate rule against fresh data inside a
/// transaction immediately before saving - see <see cref="CreateRequestAsync"/>.
/// </summary>
public class InsuranceRequestService : IInsuranceRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InsuranceRequestService> _logger;

    public InsuranceRequestService(ApplicationDbContext context, ILogger<InsuranceRequestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =========================================================== Patient side

    public async Task<Result<PagedResult<PatientInsuranceRequestDto>>> GetPatientRequestsAsync(
        string patientUserId,
        PatientInsuranceRequestQueryParameters query,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PagedResult<PatientInsuranceRequestDto>>.NotFound(
                "Patient profile not found for the current account.");
        }

        var requests = _context.InsuranceRequests
            .AsNoTracking()
            .Where(r => r.PatientProfileId == patientProfileId.Value);

        if (query.Status.HasValue)
        {
            requests = requests.Where(r => r.Status == query.Status.Value);
        }

        if (query.DateFrom.HasValue)
        {
            var from = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            requests = requests.Where(r => r.SubmittedAt >= from);
        }

        if (query.DateTo.HasValue)
        {
            var to = query.DateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            requests = requests.Where(r => r.SubmittedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(query.HospitalName))
        {
            var term = query.HospitalName.Trim();
            requests = requests.Where(r =>
                r.HospitalProfile!.HospitalName != null &&
                EF.Functions.Like(r.HospitalProfile.HospitalName, $"%{term}%"));
        }

        if (query.InsuranceCompanyId.HasValue)
        {
            requests = requests.Where(r => r.InsuranceCompanyId == query.InsuranceCompanyId.Value);
        }

        var totalCount = await requests.CountAsync(ct);

        var items = await requests
            .OrderByDescending(r => r.SubmittedAt)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(PatientProjection())
            .ToListAsync(ct);

        return Result<PagedResult<PatientInsuranceRequestDto>>.Success(
            PagedResult<PatientInsuranceRequestDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Insurance requests retrieved successfully.");
    }

    public async Task<Result<PatientInsuranceRequestDto>> GetPatientRequestByIdAsync(
        string patientUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PatientInsuranceRequestDto>.NotFound(
                "Patient profile not found for the current account.");
        }

        // Filtering by the owning patient id up front means a mismatched id and a missing
        // row both come back as the same "not found" - no oracle for probing other
        // patients' request ids.
        var request = await _context.InsuranceRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId && r.PatientProfileId == patientProfileId.Value)
            .Select(PatientProjection())
            .FirstOrDefaultAsync(ct);

        return request is null
            ? Result<PatientInsuranceRequestDto>.NotFound("Insurance request not found.")
            : Result<PatientInsuranceRequestDto>.Success(request, "Insurance request retrieved successfully.");
    }

    public async Task<Result<PatientInsuranceRequestDto>> CreateRequestAsync(
        string patientUserId,
        CreateInsuranceRequestRequest request,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PatientInsuranceRequestDto>.NotFound(
                "Patient profile not found for the current account.");
        }

        var appointment = await _context.Appointments
            .Include(a => a.HospitalProfile).ThenInclude(h => h!.User)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

        // Rules 3 and 4: missing and "belongs to someone else" both look identical from the
        // outside, so a patient cannot probe for other people's appointment ids this way.
        if (appointment is null || appointment.PatientProfileId != patientProfileId.Value)
        {
            return Result<PatientInsuranceRequestDto>.NotFound("Appointment not found.");
        }

        // Rules 5 and 6: the hospital always comes from the appointment - the request DTO
        // has no HospitalProfileId field for the client to send one.
        var hospital = appointment.HospitalProfile;

        // Rule 7: only an appointment that might still happen is eligible.
        if (appointment.Status is not (AppointmentStatus.Pending or AppointmentStatus.Confirmed))
        {
            return Result<PatientInsuranceRequestDto>.Invalid(
                $"An insurance request cannot be submitted for a {appointment.Status} appointment.");
        }

        // Rules 8 and 9.
        if (hospital?.User?.IsActive != true)
        {
            return Result<PatientInsuranceRequestDto>.Invalid("This hospital's account is not currently active.");
        }

        if (!hospital.IsProfileCompleted)
        {
            return Result<PatientInsuranceRequestDto>.Invalid("This hospital has not finished setting up its profile.");
        }

        // Rule 10.
        var insuranceCompany = await _context.InsuranceCompanies
            .FirstOrDefaultAsync(c => c.Id == request.InsuranceCompanyId, ct);

        if (insuranceCompany is null)
        {
            return Result<PatientInsuranceRequestDto>.NotFound("Insurance company not found.");
        }

        if (!insuranceCompany.IsActive)
        {
            return Result<PatientInsuranceRequestDto>.Invalid(
                "This insurance company is not currently available for new requests.");
        }

        // Section 5: one live request per appointment. A rejected or cancelled one frees
        // the appointment up for a fresh submission.
        var blocking = await _context.InsuranceRequests
            .Where(r => r.AppointmentId == appointment.Id
                        && (r.Status == InsuranceRequestStatus.Pending
                            || r.Status == InsuranceRequestStatus.UnderReview
                            || r.Status == InsuranceRequestStatus.Approved))
            .FirstOrDefaultAsync(ct);

        if (blocking is not null)
        {
            return Result<PatientInsuranceRequestDto>.Conflict(
                blocking.Status == InsuranceRequestStatus.Approved
                    ? "This appointment already has an approved insurance request."
                    : "This appointment already has an active insurance request awaiting a decision.");
        }

        var insuranceRequest = new InsuranceRequest
        {
            PatientProfileId = patientProfileId.Value,
            HospitalProfileId = hospital.Id,
            AppointmentId = appointment.Id,
            InsuranceCompanyId = insuranceCompany.Id,
            MemberNumber = request.MemberNumber.Trim(),
            PolicyNumber = Normalise(request.PolicyNumber),
            ServiceDescription = request.ServiceDescription.Trim(),
            RequestedAmount = request.RequestedAmount,
            PatientNotes = Normalise(request.PatientNotes),
            InsuranceCardImageUrl = Normalise(request.InsuranceCardImageUrl),
            SupportingDocumentUrl = Normalise(request.SupportingDocumentUrl),
            Status = InsuranceRequestStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.InsuranceRequests.Add(insuranceRequest);

        // Rule 5 of section 5: the duplicate rule is re-checked immediately before saving,
        // inside a transaction, and the filtered unique index on AppointmentId is the last
        // line of defence if two submissions for the same appointment land together.
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var stillFree = !await _context.InsuranceRequests
                .AsNoTracking()
                .AnyAsync(r => r.Id != insuranceRequest.Id
                               && r.AppointmentId == appointment.Id
                               && (r.Status == InsuranceRequestStatus.Pending
                                   || r.Status == InsuranceRequestStatus.UnderReview
                                   || r.Status == InsuranceRequestStatus.Approved),
                    ct);

            if (!stillFree)
            {
                return Result<PatientInsuranceRequestDto>.Conflict(
                    "This appointment already has an active insurance request awaiting a decision.");
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "Duplicate insurance request race detected for appointment {AppointmentId}.",
                appointment.Id);

            return Result<PatientInsuranceRequestDto>.Conflict(
                "This appointment already has an active insurance request awaiting a decision.");
        }

        _logger.LogInformation(
            "Patient {PatientProfileId} submitted insurance request {RequestId} for appointment {AppointmentId}.",
            patientProfileId, insuranceRequest.Id, appointment.Id);

        var dto = await _context.InsuranceRequests
            .AsNoTracking()
            .Where(r => r.Id == insuranceRequest.Id)
            .Select(PatientProjection())
            .FirstAsync(ct);

        return Result<PatientInsuranceRequestDto>.Success(dto, "Insurance request submitted successfully.");
    }

    public async Task<Result<PatientInsuranceRequestDto>> CancelRequestAsync(
        string patientUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PatientInsuranceRequestDto>.NotFound(
                "Patient profile not found for the current account.");
        }

        var insuranceRequest = await _context.InsuranceRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (insuranceRequest is null || insuranceRequest.PatientProfileId != patientProfileId.Value)
        {
            return Result<PatientInsuranceRequestDto>.NotFound("Insurance request not found.");
        }

        if (insuranceRequest.Status != InsuranceRequestStatus.Pending)
        {
            return Result<PatientInsuranceRequestDto>.Invalid(
                $"Only pending requests can be cancelled. This request is {insuranceRequest.Status}.");
        }

        insuranceRequest.Status = InsuranceRequestStatus.Cancelled;
        insuranceRequest.CancelledAt = DateTime.UtcNow;
        insuranceRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Patient {PatientProfileId} cancelled insurance request {RequestId}.",
            patientProfileId, requestId);

        var dto = await _context.InsuranceRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(PatientProjection())
            .FirstAsync(ct);

        return Result<PatientInsuranceRequestDto>.Success(dto, "Insurance request cancelled successfully.");
    }

    public async Task<Result<PatientInsuranceDashboardStatsDto>> GetPatientDashboardStatsAsync(
        string patientUserId,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PatientInsuranceDashboardStatsDto>.NotFound(
                "Patient profile not found for the current account.");
        }

        var pendingCount = await _context.InsuranceRequests.AsNoTracking().CountAsync(
            r => r.PatientProfileId == patientProfileId.Value && r.Status == InsuranceRequestStatus.Pending, ct);

        var approvedCount = await _context.InsuranceRequests.AsNoTracking().CountAsync(
            r => r.PatientProfileId == patientProfileId.Value && r.Status == InsuranceRequestStatus.Approved, ct);

        var latestStatus = await _context.InsuranceRequests
            .AsNoTracking()
            .Where(r => r.PatientProfileId == patientProfileId.Value)
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => (InsuranceRequestStatus?)r.Status)
            .FirstOrDefaultAsync(ct);

        return Result<PatientInsuranceDashboardStatsDto>.Success(
            new PatientInsuranceDashboardStatsDto
            {
                PendingCount = pendingCount,
                ApprovedCount = approvedCount,
                LatestStatus = latestStatus?.ToString()
            },
            "Dashboard statistics retrieved successfully.");
    }

    // ========================================================= Hospital side

    public async Task<Result<PagedResult<HospitalInsuranceRequestDto>>> GetHospitalRequestsAsync(
        string hospitalUserId,
        HospitalInsuranceRequestQueryParameters query,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<PagedResult<HospitalInsuranceRequestDto>>.NotFound(
                "Hospital profile not found for the current account.");
        }

        var requests = FilterHospitalRequests(hospitalProfileId.Value, query);

        var totalCount = await requests.CountAsync(ct);

        var items = await requests
            .OrderByDescending(r => r.SubmittedAt)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(HospitalProjection())
            .ToListAsync(ct);

        return Result<PagedResult<HospitalInsuranceRequestDto>>.Success(
            PagedResult<HospitalInsuranceRequestDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Insurance requests retrieved successfully.");
    }

    public async Task<Result<HospitalInsuranceRequestDto>> GetHospitalRequestByIdAsync(
        string hospitalUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var (failure, _) = await LoadHospitalRequestAsync(hospitalUserId, requestId, ct);
        if (failure is not null)
        {
            return failure;
        }

        var dto = await _context.InsuranceRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(HospitalProjection())
            .FirstAsync(ct);

        return Result<HospitalInsuranceRequestDto>.Success(dto, "Insurance request retrieved successfully.");
    }

    public async Task<Result<HospitalInsuranceRequestDto>> StartReviewAsync(
        string hospitalUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var (failure, insuranceRequest) = await LoadHospitalRequestAsync(hospitalUserId, requestId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        if (insuranceRequest!.Status != InsuranceRequestStatus.Pending)
        {
            return Result<HospitalInsuranceRequestDto>.Invalid(
                $"Only pending requests can be moved to under review. This request is {insuranceRequest.Status}.");
        }

        insuranceRequest.Status = InsuranceRequestStatus.UnderReview;
        insuranceRequest.ReviewedAt = DateTime.UtcNow;
        insuranceRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<HospitalInsuranceRequestDto>.Success(
            await ReloadHospitalDtoAsync(requestId, ct), "Request moved to under review.");
    }

    public async Task<Result<HospitalInsuranceRequestDto>> ApproveAsync(
        string hospitalUserId,
        Guid requestId,
        ApproveInsuranceRequestRequest request,
        CancellationToken ct = default)
    {
        var (failure, insuranceRequest) = await LoadHospitalRequestAsync(hospitalUserId, requestId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        if (insuranceRequest!.Status is not (InsuranceRequestStatus.Pending or InsuranceRequestStatus.UnderReview))
        {
            return Result<HospitalInsuranceRequestDto>.Invalid(
                $"Only pending or under-review requests can be approved. This request is {insuranceRequest.Status}.");
        }

        // ApprovedAmount cannot exceed RequestedAmount when one was given - re-validated
        // here rather than trusted from any client-side arithmetic.
        if (request.ApprovedAmount.HasValue
            && insuranceRequest.RequestedAmount.HasValue
            && request.ApprovedAmount.Value > insuranceRequest.RequestedAmount.Value)
        {
            return Result<HospitalInsuranceRequestDto>.Invalid(
                "The approved amount cannot be greater than the requested amount.");
        }

        insuranceRequest.Status = InsuranceRequestStatus.Approved;
        insuranceRequest.ApprovedAmount = request.ApprovedAmount;
        insuranceRequest.ApprovalReferenceNumber = Normalise(request.ApprovalReferenceNumber);
        insuranceRequest.HospitalNotes = Normalise(request.HospitalNotes);
        insuranceRequest.ApprovedAt = DateTime.UtcNow;
        insuranceRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<HospitalInsuranceRequestDto>.Success(
            await ReloadHospitalDtoAsync(requestId, ct), "Insurance request approved successfully.");
    }

    public async Task<Result<HospitalInsuranceRequestDto>> RejectAsync(
        string hospitalUserId,
        Guid requestId,
        RejectInsuranceRequestRequest request,
        CancellationToken ct = default)
    {
        var (failure, insuranceRequest) = await LoadHospitalRequestAsync(hospitalUserId, requestId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        if (insuranceRequest!.Status is not (InsuranceRequestStatus.Pending or InsuranceRequestStatus.UnderReview))
        {
            return Result<HospitalInsuranceRequestDto>.Invalid(
                $"Only pending or under-review requests can be rejected. This request is {insuranceRequest.Status}.");
        }

        insuranceRequest.Status = InsuranceRequestStatus.Rejected;
        insuranceRequest.RejectionReason = request.RejectionReason.Trim();
        insuranceRequest.HospitalNotes = Normalise(request.HospitalNotes);
        insuranceRequest.RejectedAt = DateTime.UtcNow;
        insuranceRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<HospitalInsuranceRequestDto>.Success(
            await ReloadHospitalDtoAsync(requestId, ct), "Insurance request rejected successfully.");
    }

    public async Task<Result<HospitalInsuranceRequestDto>> UpdateHospitalNotesAsync(
        string hospitalUserId,
        Guid requestId,
        InsuranceHospitalNotesRequest request,
        CancellationToken ct = default)
    {
        var (failure, insuranceRequest) = await LoadHospitalRequestAsync(hospitalUserId, requestId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        // Notes only make sense while a decision is still pending; once final, the record
        // becomes read-only history.
        if (insuranceRequest!.Status is not (InsuranceRequestStatus.Pending or InsuranceRequestStatus.UnderReview))
        {
            return Result<HospitalInsuranceRequestDto>.Invalid(
                "Notes can only be edited while the request is pending or under review.");
        }

        insuranceRequest.HospitalNotes = Normalise(request.HospitalNotes);
        insuranceRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<HospitalInsuranceRequestDto>.Success(
            await ReloadHospitalDtoAsync(requestId, ct), "Notes saved successfully.");
    }

    public async Task<Result<HospitalInsuranceDashboardStatsDto>> GetHospitalDashboardStatsAsync(
        string hospitalUserId,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<HospitalInsuranceDashboardStatsDto>.NotFound(
                "Hospital profile not found for the current account.");
        }

        var today = DateTime.UtcNow;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var pendingCount = await _context.InsuranceRequests.AsNoTracking().CountAsync(
            r => r.HospitalProfileId == hospitalProfileId.Value && r.Status == InsuranceRequestStatus.Pending, ct);

        var underReviewCount = await _context.InsuranceRequests.AsNoTracking().CountAsync(
            r => r.HospitalProfileId == hospitalProfileId.Value && r.Status == InsuranceRequestStatus.UnderReview, ct);

        var approvedThisMonth = await _context.InsuranceRequests.AsNoTracking().CountAsync(
            r => r.HospitalProfileId == hospitalProfileId.Value
                 && r.Status == InsuranceRequestStatus.Approved
                 && r.ApprovedAt >= monthStart,
            ct);

        var rejectedThisMonth = await _context.InsuranceRequests.AsNoTracking().CountAsync(
            r => r.HospitalProfileId == hospitalProfileId.Value
                 && r.Status == InsuranceRequestStatus.Rejected
                 && r.RejectedAt >= monthStart,
            ct);

        return Result<HospitalInsuranceDashboardStatsDto>.Success(
            new HospitalInsuranceDashboardStatsDto
            {
                PendingCount = pendingCount,
                UnderReviewCount = underReviewCount,
                ApprovedThisMonthCount = approvedThisMonth,
                RejectedThisMonthCount = rejectedThisMonth
            },
            "Dashboard statistics retrieved successfully.");
    }

    // =============================================================== Helpers

    private Task<Guid?> GetPatientProfileIdAsync(string userId, CancellationToken ct) =>
        _context.PatientProfiles
            .Where(p => p.UserId == userId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);

    private Task<Guid?> GetHospitalProfileIdAsync(string userId, CancellationToken ct) =>
        _context.HospitalProfiles
            .Where(h => h.UserId == userId)
            .Select(h => (Guid?)h.Id)
            .FirstOrDefaultAsync(ct);

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private IQueryable<InsuranceRequest> FilterHospitalRequests(
        Guid hospitalProfileId,
        HospitalInsuranceRequestQueryParameters query)
    {
        var requests = _context.InsuranceRequests
            .AsNoTracking()
            .Where(r => r.HospitalProfileId == hospitalProfileId);

        if (query.Status.HasValue)
        {
            requests = requests.Where(r => r.Status == query.Status.Value);
        }

        if (query.DateFrom.HasValue)
        {
            var from = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            requests = requests.Where(r => r.SubmittedAt >= from);
        }

        if (query.DateTo.HasValue)
        {
            var to = query.DateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            requests = requests.Where(r => r.SubmittedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(query.PatientName))
        {
            var term = query.PatientName.Trim();
            requests = requests.Where(r => EF.Functions.Like(r.PatientProfile!.User!.FullName, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.DoctorName))
        {
            var term = query.DoctorName.Trim();
            requests = requests.Where(r =>
                EF.Functions.Like(r.Appointment!.DoctorProfile!.User!.FullName, $"%{term}%"));
        }

        if (query.InsuranceCompanyId.HasValue)
        {
            requests = requests.Where(r => r.InsuranceCompanyId == query.InsuranceCompanyId.Value);
        }

        return requests;
    }

    /// <summary>
    /// Loads a request and proves it belongs to the calling hospital. Returns the same
    /// "not found" for a missing row and for another hospital's row, so the endpoint cannot
    /// be used to probe for valid request ids.
    /// </summary>
    private async Task<(Result<HospitalInsuranceRequestDto>? Failure, InsuranceRequest? Request)>
        LoadHospitalRequestAsync(string hospitalUserId, Guid requestId, CancellationToken ct, bool tracking = false)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return (Result<HospitalInsuranceRequestDto>.NotFound(
                "Hospital profile not found for the current account."), null);
        }

        var query = _context.InsuranceRequests.Where(r => r.Id == requestId);
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        var insuranceRequest = await query.FirstOrDefaultAsync(ct);

        if (insuranceRequest is null || insuranceRequest.HospitalProfileId != hospitalProfileId.Value)
        {
            return (Result<HospitalInsuranceRequestDto>.NotFound("Insurance request not found."), null);
        }

        return (null, insuranceRequest);
    }

    private async Task<HospitalInsuranceRequestDto> ReloadHospitalDtoAsync(Guid requestId, CancellationToken ct) =>
        await _context.InsuranceRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(HospitalProjection())
            .FirstAsync(ct);

    private static Expression<Func<InsuranceRequest, PatientInsuranceRequestDto>> PatientProjection() =>
        r => new PatientInsuranceRequestDto
        {
            InsuranceRequestId = r.Id,
            AppointmentId = r.AppointmentId,
            AppointmentDate = r.Appointment!.AppointmentDate,
            AppointmentStartTime = r.Appointment.StartTime.ToString("HH:mm:ss"),
            DoctorName = r.Appointment.DoctorProfile!.User!.FullName,
            DoctorSpecialty = r.Appointment.DoctorProfile.Specialty != null
                ? r.Appointment.DoctorProfile.Specialty.Name
                : null,
            HospitalName = r.HospitalProfile!.HospitalName ?? string.Empty,
            InsuranceCompanyName = r.InsuranceCompany!.Name,
            MemberNumber = r.MemberNumber,
            ServiceDescription = r.ServiceDescription,
            RequestedAmount = r.RequestedAmount,
            ApprovedAmount = r.ApprovedAmount,
            Status = r.Status,
            StatusName = r.Status.ToString(),
            RejectionReason = r.RejectionReason,
            ApprovalReferenceNumber = r.ApprovalReferenceNumber,
            SubmittedAt = r.SubmittedAt,
            ReviewedAt = r.ReviewedAt,
            ApprovedAt = r.ApprovedAt,
            RejectedAt = r.RejectedAt,
            CancelledAt = r.CancelledAt
        };

    private static Expression<Func<InsuranceRequest, HospitalInsuranceRequestDto>> HospitalProjection() =>
        r => new HospitalInsuranceRequestDto
        {
            InsuranceRequestId = r.Id,
            PatientProfileId = r.PatientProfileId,
            PatientName = r.PatientProfile!.User!.FullName,
            PatientPhoneNumber = r.PatientProfile.User.PhoneNumber,
            AppointmentId = r.AppointmentId,
            AppointmentDate = r.Appointment!.AppointmentDate,
            AppointmentStartTime = r.Appointment.StartTime.ToString("HH:mm:ss"),
            DoctorName = r.Appointment.DoctorProfile!.User!.FullName,
            DoctorSpecialty = r.Appointment.DoctorProfile.Specialty != null
                ? r.Appointment.DoctorProfile.Specialty.Name
                : null,
            InsuranceCompanyId = r.InsuranceCompanyId,
            InsuranceCompany = r.InsuranceCompany!.Name,
            MemberNumber = r.MemberNumber,
            PolicyNumber = r.PolicyNumber,
            ServiceDescription = r.ServiceDescription,
            RequestedAmount = r.RequestedAmount,
            ApprovedAmount = r.ApprovedAmount,
            PatientNotes = r.PatientNotes,
            HospitalNotes = r.HospitalNotes,
            InsuranceCardImageUrl = r.InsuranceCardImageUrl,
            SupportingDocumentUrl = r.SupportingDocumentUrl,
            Status = r.Status,
            StatusName = r.Status.ToString(),
            RejectionReason = r.RejectionReason,
            ApprovalReferenceNumber = r.ApprovalReferenceNumber,
            SubmittedAt = r.SubmittedAt,
            ReviewedAt = r.ReviewedAt,
            ApprovedAt = r.ApprovedAt,
            RejectedAt = r.RejectedAt
        };
}
