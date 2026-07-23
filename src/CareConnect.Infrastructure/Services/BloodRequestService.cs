using System.Linq.Expressions;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.BloodRequests;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Owns the patient blood-request lifecycle for both sides - Patient and Hospital.
/// Ownership always comes from the caller's user id, never from a route or body id.
/// Approving a request is the only operation that touches <see cref="BloodStock"/>, and it
/// does so inside a transaction that re-reads the stock row fresh - see
/// <see cref="ApproveAsync"/>.
/// </summary>
public class BloodRequestService : IBloodRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BloodRequestService> _logger;

    public BloodRequestService(ApplicationDbContext context, ILogger<BloodRequestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =========================================================== Patient side

    public async Task<Result<PagedResult<PatientBloodRequestDto>>> GetPatientRequestsAsync(
        string patientUserId,
        PatientBloodRequestQueryParameters query,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PagedResult<PatientBloodRequestDto>>.NotFound(
                "Patient profile not found for the current account.");
        }

        var requests = _context.BloodRequests
            .AsNoTracking()
            .Where(r => r.PatientProfileId == patientProfileId.Value);

        if (query.Status.HasValue)
        {
            requests = requests.Where(r => r.Status == query.Status.Value);
        }

        if (query.BloodGroup.HasValue)
        {
            requests = requests.Where(r => r.BloodGroup == query.BloodGroup.Value);
        }

        if (query.Urgency.HasValue)
        {
            requests = requests.Where(r => r.Urgency == query.Urgency.Value);
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

        var totalCount = await requests.CountAsync(ct);

        var items = await requests
            .OrderByDescending(r => r.SubmittedAt)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(PatientProjection())
            .ToListAsync(ct);

        return Result<PagedResult<PatientBloodRequestDto>>.Success(
            PagedResult<PatientBloodRequestDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Blood requests retrieved successfully.");
    }

    public async Task<Result<PatientBloodRequestDto>> GetPatientRequestByIdAsync(
        string patientUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PatientBloodRequestDto>.NotFound(
                "Patient profile not found for the current account.");
        }

        // Filtering by the owning patient id up front means a mismatched id and a missing
        // row both come back as the same "not found" - no oracle for probing other
        // patients' request ids.
        var request = await _context.BloodRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId && r.PatientProfileId == patientProfileId.Value)
            .Select(PatientProjection())
            .FirstOrDefaultAsync(ct);

        return request is null
            ? Result<PatientBloodRequestDto>.NotFound("Blood request not found.")
            : Result<PatientBloodRequestDto>.Success(request, "Blood request retrieved successfully.");
    }

    public async Task<Result<PatientBloodRequestDto>> CreateRequestAsync(
        string patientUserId,
        CreateBloodRequestRequest request,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PatientBloodRequestDto>.NotFound(
                "Patient profile not found for the current account.");
        }

        var hospital = await _context.HospitalProfiles
            .Include(h => h.User)
            .FirstOrDefaultAsync(h => h.Id == request.HospitalProfileId, ct);

        if (hospital is null)
        {
            return Result<PatientBloodRequestDto>.NotFound("Hospital not found.");
        }

        if (hospital.User?.IsActive != true)
        {
            return Result<PatientBloodRequestDto>.Invalid("This hospital's account is not currently active.");
        }

        if (!hospital.IsProfileCompleted)
        {
            return Result<PatientBloodRequestDto>.Invalid("This hospital has not finished setting up its profile.");
        }

        var stock = await _context.BloodStocks.FirstOrDefaultAsync(
            s => s.HospitalProfileId == hospital.Id && s.BloodGroup == request.BloodGroup, ct);

        if (stock is null)
        {
            return Result<PatientBloodRequestDto>.NotFound(
                $"This hospital does not currently stock {request.BloodGroup.ToDisplayName()}.");
        }

        // A request may still be submitted when AvailableUnits is lower than
        // UnitsRequested (the hospital reviews and decides), but there must be at least
        // some stock to request against.
        if (stock.AvailableUnits <= 0)
        {
            return Result<PatientBloodRequestDto>.Invalid(
                $"{request.BloodGroup.ToDisplayName()} is not currently available at this hospital.");
        }

        var normalizedBeneficiary = request.BeneficiaryName.Trim();

        if (await HasDuplicateActiveRequestAsync(patientProfileId.Value, hospital.Id, request.BloodGroup,
                normalizedBeneficiary, ct))
        {
            return Result<PatientBloodRequestDto>.Conflict(
                "A similar blood request for this beneficiary is already active at this hospital.");
        }

        var bloodRequest = new BloodRequest
        {
            PatientProfileId = patientProfileId.Value,
            HospitalProfileId = hospital.Id,
            BloodStockId = stock.Id,
            BloodGroup = request.BloodGroup,
            UnitsRequested = request.UnitsRequested,
            BeneficiaryName = normalizedBeneficiary,
            BeneficiaryAge = request.BeneficiaryAge,
            ContactPhoneNumber = request.ContactPhoneNumber.Trim(),
            MedicalCondition = Normalise(request.MedicalCondition),
            HospitalOrFacilityName = Normalise(request.HospitalOrFacilityName),
            RequestNotes = Normalise(request.RequestNotes),
            Urgency = request.Urgency,
            Status = BloodRequestStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        // Recheck the duplicate rule immediately before saving, so a race between two
        // near-simultaneous submissions cannot both slip through.
        if (await HasDuplicateActiveRequestAsync(patientProfileId.Value, hospital.Id, request.BloodGroup,
                normalizedBeneficiary, ct))
        {
            return Result<PatientBloodRequestDto>.Conflict(
                "A similar blood request for this beneficiary is already active at this hospital.");
        }

        _context.BloodRequests.Add(bloodRequest);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Patient {PatientProfileId} submitted blood request {RequestId} to hospital {HospitalProfileId}.",
            patientProfileId, bloodRequest.Id, hospital.Id);

        var dto = await _context.BloodRequests
            .AsNoTracking()
            .Where(r => r.Id == bloodRequest.Id)
            .Select(PatientProjection())
            .FirstAsync(ct);

        return Result<PatientBloodRequestDto>.Success(dto, "Blood request submitted successfully.");
    }

    public async Task<Result<PatientBloodRequestDto>> CancelRequestAsync(
        string patientUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PatientBloodRequestDto>.NotFound(
                "Patient profile not found for the current account.");
        }

        var bloodRequest = await _context.BloodRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (bloodRequest is null || bloodRequest.PatientProfileId != patientProfileId.Value)
        {
            return Result<PatientBloodRequestDto>.NotFound("Blood request not found.");
        }

        if (bloodRequest.Status != BloodRequestStatus.Pending)
        {
            return Result<PatientBloodRequestDto>.Invalid(
                $"Only pending requests can be cancelled. This request is {bloodRequest.Status}.");
        }

        // Cancelling a Pending request never touches stock - nothing was allocated yet.
        bloodRequest.Status = BloodRequestStatus.Cancelled;
        bloodRequest.CancelledAt = DateTime.UtcNow;
        bloodRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Patient {PatientProfileId} cancelled blood request {RequestId}.",
            patientProfileId, requestId);

        var dto = await _context.BloodRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(PatientProjection())
            .FirstAsync(ct);

        return Result<PatientBloodRequestDto>.Success(dto, "Blood request cancelled successfully.");
    }

    public async Task<Result<PatientBloodDashboardStatsDto>> GetPatientDashboardStatsAsync(
        string patientUserId,
        CancellationToken ct = default)
    {
        var patientProfileId = await GetPatientProfileIdAsync(patientUserId, ct);
        if (patientProfileId is null)
        {
            return Result<PatientBloodDashboardStatsDto>.NotFound(
                "Patient profile not found for the current account.");
        }

        var pendingCount = await _context.BloodRequests.AsNoTracking().CountAsync(
            r => r.PatientProfileId == patientProfileId.Value && r.Status == BloodRequestStatus.Pending, ct);

        var approvedCount = await _context.BloodRequests.AsNoTracking().CountAsync(
            r => r.PatientProfileId == patientProfileId.Value && r.Status == BloodRequestStatus.Approved, ct);

        var latestStatus = await _context.BloodRequests
            .AsNoTracking()
            .Where(r => r.PatientProfileId == patientProfileId.Value)
            .OrderByDescending(r => r.SubmittedAt)
            .Select(r => (BloodRequestStatus?)r.Status)
            .FirstOrDefaultAsync(ct);

        return Result<PatientBloodDashboardStatsDto>.Success(
            new PatientBloodDashboardStatsDto
            {
                PendingCount = pendingCount,
                ApprovedCount = approvedCount,
                LatestStatus = latestStatus?.ToString()
            },
            "Dashboard statistics retrieved successfully.");
    }

    // ========================================================= Hospital side

    public async Task<Result<PagedResult<HospitalBloodRequestDto>>> GetHospitalRequestsAsync(
        string hospitalUserId,
        HospitalBloodRequestQueryParameters query,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<PagedResult<HospitalBloodRequestDto>>.NotFound(
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

        return Result<PagedResult<HospitalBloodRequestDto>>.Success(
            PagedResult<HospitalBloodRequestDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Blood requests retrieved successfully.");
    }

    public async Task<Result<HospitalBloodRequestDto>> GetHospitalRequestByIdAsync(
        string hospitalUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var (failure, _) = await LoadHospitalRequestAsync(hospitalUserId, requestId, ct);
        if (failure is not null)
        {
            return failure;
        }

        var dto = await _context.BloodRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(HospitalProjection())
            .FirstAsync(ct);

        return Result<HospitalBloodRequestDto>.Success(dto, "Blood request retrieved successfully.");
    }

    public async Task<Result<HospitalBloodRequestDto>> ApproveAsync(
        string hospitalUserId,
        Guid requestId,
        ApproveBloodRequestRequest request,
        CancellationToken ct = default)
    {
        var (failure, bloodRequest) = await LoadHospitalRequestAsync(hospitalUserId, requestId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        if (bloodRequest!.Status != BloodRequestStatus.Pending)
        {
            return Result<HospitalBloodRequestDto>.Invalid(
                $"Only pending requests can be approved. This request is {bloodRequest.Status}.");
        }

        // Allocating stock and approving the request happen together, in one transaction,
        // against a freshly re-read stock row - never against whatever Angular last displayed.
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var stock = await _context.BloodStocks.FirstAsync(s => s.Id == bloodRequest.BloodStockId, ct);

            if (stock.AvailableUnits < bloodRequest.UnitsRequested)
            {
                await transaction.RollbackAsync(ct);
                return Result<HospitalBloodRequestDto>.Conflict(
                    $"Only {stock.AvailableUnits} units of {bloodRequest.BloodGroup.ToDisplayName()} are " +
                    $"available - {bloodRequest.UnitsRequested} were requested.");
            }

            stock.AvailableUnits -= bloodRequest.UnitsRequested;
            stock.IsAvailable = stock.AvailableUnits > 0;
            stock.LastUpdatedByUserId = hospitalUserId;
            stock.UpdatedAt = DateTime.UtcNow;

            bloodRequest.Status = BloodRequestStatus.Approved;
            bloodRequest.HospitalNotes = Normalise(request.HospitalNotes);
            bloodRequest.ApprovedAt = DateTime.UtcNow;
            bloodRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "Concurrent stock update detected while approving blood request {RequestId}.",
                requestId);

            return Result<HospitalBloodRequestDto>.Conflict(
                "Blood stock changed while approving this request. Please try again.");
        }

        _logger.LogInformation(
            "Hospital {HospitalProfileId} approved blood request {RequestId}, allocating {Units} units.",
            bloodRequest.HospitalProfileId, requestId, bloodRequest.UnitsRequested);

        return Result<HospitalBloodRequestDto>.Success(
            await ReloadHospitalDtoAsync(requestId, ct), "Blood request approved successfully.");
    }

    public async Task<Result<HospitalBloodRequestDto>> RejectAsync(
        string hospitalUserId,
        Guid requestId,
        RejectBloodRequestRequest request,
        CancellationToken ct = default)
    {
        var (failure, bloodRequest) = await LoadHospitalRequestAsync(hospitalUserId, requestId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        if (bloodRequest!.Status != BloodRequestStatus.Pending)
        {
            return Result<HospitalBloodRequestDto>.Invalid(
                $"Only pending requests can be rejected. This request is {bloodRequest.Status}.");
        }

        // Rejecting a Pending request never touches stock - nothing was allocated yet.
        bloodRequest.Status = BloodRequestStatus.Rejected;
        bloodRequest.RejectionReason = request.RejectionReason.Trim();
        bloodRequest.HospitalNotes = Normalise(request.HospitalNotes);
        bloodRequest.RejectedAt = DateTime.UtcNow;
        bloodRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<HospitalBloodRequestDto>.Success(
            await ReloadHospitalDtoAsync(requestId, ct), "Blood request rejected successfully.");
    }

    public async Task<Result<HospitalBloodRequestDto>> FulfillAsync(
        string hospitalUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var (failure, bloodRequest) = await LoadHospitalRequestAsync(hospitalUserId, requestId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        if (bloodRequest!.Status != BloodRequestStatus.Approved)
        {
            return Result<HospitalBloodRequestDto>.Invalid(
                $"Only approved requests can be marked fulfilled. This request is {bloodRequest.Status}.");
        }

        // Units were already removed from stock at approval time - fulfillment only marks
        // the hand-off complete and must never decrement stock again.
        bloodRequest.Status = BloodRequestStatus.Fulfilled;
        bloodRequest.FulfilledAt = DateTime.UtcNow;
        bloodRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<HospitalBloodRequestDto>.Success(
            await ReloadHospitalDtoAsync(requestId, ct), "Blood request marked as fulfilled.");
    }

    public async Task<Result<HospitalBloodRequestDto>> UpdateHospitalNotesAsync(
        string hospitalUserId,
        Guid requestId,
        BloodRequestHospitalNotesRequest request,
        CancellationToken ct = default)
    {
        var (failure, bloodRequest) = await LoadHospitalRequestAsync(hospitalUserId, requestId, ct, tracking: true);
        if (failure is not null)
        {
            return failure;
        }

        if (bloodRequest!.Status is not (BloodRequestStatus.Pending or BloodRequestStatus.Approved))
        {
            return Result<HospitalBloodRequestDto>.Invalid(
                "Notes can only be edited while the request is pending or approved.");
        }

        bloodRequest.HospitalNotes = Normalise(request.HospitalNotes);
        bloodRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<HospitalBloodRequestDto>.Success(
            await ReloadHospitalDtoAsync(requestId, ct), "Notes saved successfully.");
    }

    public async Task<Result<HospitalBloodDashboardStatsDto>> GetHospitalDashboardStatsAsync(
        string hospitalUserId,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<HospitalBloodDashboardStatsDto>.NotFound(
                "Hospital profile not found for the current account.");
        }

        var totalAvailableUnits = await _context.BloodStocks
            .AsNoTracking()
            .Where(s => s.HospitalProfileId == hospitalProfileId.Value)
            .SumAsync(s => (int?)s.AvailableUnits, ct) ?? 0;

        var belowMinimumCount = await _context.BloodStocks.AsNoTracking().CountAsync(
            s => s.HospitalProfileId == hospitalProfileId.Value && s.AvailableUnits < s.MinimumRequiredUnits, ct);

        var pendingCount = await _context.BloodRequests.AsNoTracking().CountAsync(
            r => r.HospitalProfileId == hospitalProfileId.Value && r.Status == BloodRequestStatus.Pending, ct);

        var emergencyCount = await _context.BloodRequests.AsNoTracking().CountAsync(
            r => r.HospitalProfileId == hospitalProfileId.Value
                 && r.Status == BloodRequestStatus.Pending
                 && r.Urgency == BloodRequestUrgency.Emergency,
            ct);

        var approvedAwaitingFulfillment = await _context.BloodRequests.AsNoTracking().CountAsync(
            r => r.HospitalProfileId == hospitalProfileId.Value && r.Status == BloodRequestStatus.Approved, ct);

        return Result<HospitalBloodDashboardStatsDto>.Success(
            new HospitalBloodDashboardStatsDto
            {
                TotalAvailableUnits = totalAvailableUnits,
                BloodGroupsBelowMinimumCount = belowMinimumCount,
                PendingRequestsCount = pendingCount,
                EmergencyRequestsCount = emergencyCount,
                ApprovedAwaitingFulfillmentCount = approvedAwaitingFulfillment
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

    /// <summary>
    /// Pending or Approved, same hospital, same blood group, same (case-insensitive,
    /// trimmed) beneficiary name, submitted in the previous 24 hours. Deliberately simple -
    /// no fuzzy identity matching.
    /// </summary>
    private Task<bool> HasDuplicateActiveRequestAsync(
        Guid patientProfileId, Guid hospitalProfileId, BloodGroup bloodGroup, string beneficiaryName,
        CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var normalizedName = beneficiaryName.Trim().ToLower();

        return _context.BloodRequests.AsNoTracking().AnyAsync(r =>
            r.PatientProfileId == patientProfileId
            && r.HospitalProfileId == hospitalProfileId
            && r.BloodGroup == bloodGroup
            && r.BeneficiaryName.ToLower() == normalizedName
            && r.SubmittedAt >= since
            && (r.Status == BloodRequestStatus.Pending || r.Status == BloodRequestStatus.Approved),
            ct);
    }

    private IQueryable<BloodRequest> FilterHospitalRequests(
        Guid hospitalProfileId,
        HospitalBloodRequestQueryParameters query)
    {
        var requests = _context.BloodRequests
            .AsNoTracking()
            .Where(r => r.HospitalProfileId == hospitalProfileId);

        if (query.Status.HasValue)
        {
            requests = requests.Where(r => r.Status == query.Status.Value);
        }

        if (query.BloodGroup.HasValue)
        {
            requests = requests.Where(r => r.BloodGroup == query.BloodGroup.Value);
        }

        if (query.Urgency.HasValue)
        {
            requests = requests.Where(r => r.Urgency == query.Urgency.Value);
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

        if (!string.IsNullOrWhiteSpace(query.BeneficiaryName))
        {
            var term = query.BeneficiaryName.Trim();
            requests = requests.Where(r => EF.Functions.Like(r.BeneficiaryName, $"%{term}%"));
        }

        return requests;
    }

    /// <summary>
    /// Loads a request and proves it belongs to the calling hospital. Returns the same
    /// "not found" for a missing row and for another hospital's row, so the endpoint cannot
    /// be used to probe for valid request ids.
    /// </summary>
    private async Task<(Result<HospitalBloodRequestDto>? Failure, BloodRequest? Request)> LoadHospitalRequestAsync(
        string hospitalUserId, Guid requestId, CancellationToken ct, bool tracking = false)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return (Result<HospitalBloodRequestDto>.NotFound(
                "Hospital profile not found for the current account."), null);
        }

        var query = _context.BloodRequests.Where(r => r.Id == requestId);
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        var bloodRequest = await query.FirstOrDefaultAsync(ct);

        if (bloodRequest is null || bloodRequest.HospitalProfileId != hospitalProfileId.Value)
        {
            return (Result<HospitalBloodRequestDto>.NotFound("Blood request not found."), null);
        }

        return (null, bloodRequest);
    }

    private async Task<HospitalBloodRequestDto> ReloadHospitalDtoAsync(Guid requestId, CancellationToken ct) =>
        await _context.BloodRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(HospitalProjection())
            .FirstAsync(ct);

    private static Expression<Func<BloodRequest, PatientBloodRequestDto>> PatientProjection() =>
        r => new PatientBloodRequestDto
        {
            BloodRequestId = r.Id,
            HospitalProfileId = r.HospitalProfileId,
            HospitalName = r.HospitalProfile!.HospitalName ?? string.Empty,
            HospitalAddress = r.HospitalProfile.Address,
            HospitalPhoneNumber = r.HospitalProfile.PhoneNumber,
            BloodGroup = r.BloodGroup,
            BloodGroupDisplayName = r.BloodGroup.ToDisplayName(),
            UnitsRequested = r.UnitsRequested,
            BeneficiaryName = r.BeneficiaryName,
            ContactPhoneNumber = r.ContactPhoneNumber,
            Urgency = r.Urgency,
            UrgencyName = r.Urgency.ToString(),
            Status = r.Status,
            StatusName = r.Status.ToString(),
            RejectionReason = r.RejectionReason,
            HospitalNotes = r.HospitalNotes,
            SubmittedAt = r.SubmittedAt,
            ApprovedAt = r.ApprovedAt,
            RejectedAt = r.RejectedAt,
            FulfilledAt = r.FulfilledAt,
            CancelledAt = r.CancelledAt
        };

    private static Expression<Func<BloodRequest, HospitalBloodRequestDto>> HospitalProjection() =>
        r => new HospitalBloodRequestDto
        {
            BloodRequestId = r.Id,
            PatientProfileId = r.PatientProfileId,
            PatientName = r.PatientProfile!.User!.FullName,
            PatientPhoneNumber = r.PatientProfile.User.PhoneNumber,
            BeneficiaryName = r.BeneficiaryName,
            BeneficiaryAge = r.BeneficiaryAge,
            ContactPhoneNumber = r.ContactPhoneNumber,
            BloodGroup = r.BloodGroup,
            BloodGroupDisplayName = r.BloodGroup.ToDisplayName(),
            UnitsRequested = r.UnitsRequested,
            CurrentAvailableUnits = r.BloodStock!.AvailableUnits,
            MedicalCondition = r.MedicalCondition,
            HospitalOrFacilityName = r.HospitalOrFacilityName,
            RequestNotes = r.RequestNotes,
            HospitalNotes = r.HospitalNotes,
            Urgency = r.Urgency,
            UrgencyName = r.Urgency.ToString(),
            Status = r.Status,
            StatusName = r.Status.ToString(),
            RejectionReason = r.RejectionReason,
            SubmittedAt = r.SubmittedAt,
            ApprovedAt = r.ApprovedAt,
            RejectedAt = r.RejectedAt,
            FulfilledAt = r.FulfilledAt,
            CancelledAt = r.CancelledAt
        };
}
