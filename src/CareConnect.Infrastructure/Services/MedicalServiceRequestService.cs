using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Security.Cryptography;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceRequests;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Domain.Rules;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// Owns both sides of the medical-service-request workflow. Every public method resolves
/// the caller's profile from their authenticated user id and never trusts a client-supplied
/// owner id.
/// </summary>
public sealed class MedicalServiceRequestService : IMedicalServiceRequestService
{
    private static readonly TimeZoneInfo EgyptTimeZone = ResolveEgyptTimeZone();

    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicalServiceRequestService> _logger;

    public MedicalServiceRequestService(
        ApplicationDbContext context,
        ILogger<MedicalServiceRequestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ================================================================ Patient

    public async Task<Result<MedicalServiceRequestDetailsDto>> CreateAsync(
        string patientUserId,
        CreateMedicalServiceRequestRequest request,
        CancellationToken ct = default)
    {
        var patient = await _context.PatientProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == patientUserId)
            .Select(profile => new { profile.Id, IsActive = profile.User!.IsActive })
            .FirstOrDefaultAsync(ct);

        if (patient is null)
        {
            return Result<MedicalServiceRequestDetailsDto>.NotFound(
                "Patient profile not found for the current account.");
        }

        if (!patient.IsActive)
        {
            return Result<MedicalServiceRequestDetailsDto>.Failure(
                ResultStatus.Forbidden,
                "Your account is inactive.");
        }

        if (!TimeOnly.TryParse(request.PreferredStartTime, CultureInfo.InvariantCulture, out var startTime))
        {
            return Result<MedicalServiceRequestDetailsDto>.Invalid(
                "Preferred start time is invalid.");
        }

        var offering = await _context.MedicalServiceOfferings
            .AsNoTracking()
            .Where(service => service.Id == request.MedicalServiceOfferingId)
            .Select(service => new
            {
                service.Id,
                service.MedicalServiceProviderProfileId,
                service.Name,
                service.Price,
                service.EstimatedDurationMinutes,
                service.DeliveryModeAvailability,
                service.IsActive,
                CategoryName = service.MedicalServiceCategory!.Name,
                CategoryIsActive = service.MedicalServiceCategory.IsActive,
                ProviderPublished = service.MedicalServiceProviderProfile!.IsPublished,
                ProviderActive = service.MedicalServiceProviderProfile.User!.IsActive
            })
            .FirstOrDefaultAsync(ct);

        if (offering is null)
        {
            return Result<MedicalServiceRequestDetailsDto>.NotFound("Medical service not found.");
        }

        if (!offering.ProviderActive || !offering.ProviderPublished)
        {
            return Result<MedicalServiceRequestDetailsDto>.Invalid(
                "This medical service provider is not currently accepting public requests.");
        }

        if (!offering.IsActive || !offering.CategoryIsActive)
        {
            return Result<MedicalServiceRequestDetailsDto>.Invalid(
                "This medical service is not currently available.");
        }

        if (!SupportsDeliveryMode(offering.DeliveryModeAvailability, request.DeliveryMode))
        {
            return Result<MedicalServiceRequestDetailsDto>.Invalid(
                "The selected delivery mode is not supported by this service.");
        }

        var homeAddress = request.DeliveryMode == ServiceDeliveryMode.HomeVisit
            ? Normalise(request.HomeVisitAddress)
            : null;
        if (request.DeliveryMode == ServiceDeliveryMode.HomeVisit && homeAddress is null)
        {
            return Result<MedicalServiceRequestDetailsDto>.Invalid(
                "A home-visit address is required.");
        }

        var dateValidation = ValidateBookableDateTime(request.RequestedDate, startTime);
        if (dateValidation is not null)
        {
            return Result<MedicalServiceRequestDetailsDto>.Invalid(dateValidation);
        }

        var hoursValidation = await ValidateWorkingHoursAsync(
            offering.MedicalServiceProviderProfileId,
            request.RequestedDate,
            startTime,
            offering.EstimatedDurationMinutes,
            ct);
        if (hoursValidation is not null)
        {
            return Result<MedicalServiceRequestDetailsDto>.Invalid(hoursValidation);
        }

        if (await ActiveDuplicateExistsAsync(
                patient.Id,
                offering.Id,
                request.RequestedDate,
                startTime,
                ct))
        {
            return Result<MedicalServiceRequestDetailsDto>.Conflict(
                "You already have an active request for this service at the selected date and time.");
        }

        var now = DateTime.UtcNow;
        var entity = new MedicalServiceRequest
        {
            RequestNumber = await GenerateRequestNumberAsync(ct),
            PatientProfileId = patient.Id,
            MedicalServiceProviderProfileId = offering.MedicalServiceProviderProfileId,
            MedicalServiceOfferingId = offering.Id,
            Status = MedicalServiceRequestStatus.Pending,
            DeliveryMode = request.DeliveryMode,
            RequestedDate = request.RequestedDate,
            PreferredStartTime = startTime,
            PatientNotes = Normalise(request.PatientNotes),
            HomeVisitAddress = homeAddress,
            ServiceNameSnapshot = offering.Name,
            CategoryNameSnapshot = offering.CategoryName,
            PriceSnapshot = offering.Price,
            DurationMinutesSnapshot = offering.EstimatedDurationMinutes,
            CreatedAt = now
        };
        entity.StatusHistory.Add(CreateHistory(
            entity,
            previousStatus: null,
            MedicalServiceRequestStatus.Pending,
            patientUserId,
            "Request submitted.",
            now));

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            ct);

        try
        {
            // Recheck immediately before saving. The filtered unique index is the final
            // guard if two requests still race between this query and SaveChanges.
            if (await ActiveDuplicateExistsAsync(
                    patient.Id,
                    offering.Id,
                    request.RequestedDate,
                    startTime,
                    ct))
            {
                await transaction.RollbackAsync(ct);
                return Result<MedicalServiceRequestDetailsDto>.Conflict(
                    "You already have an active request for this service at the selected date and time.");
            }

            _context.MedicalServiceRequests.Add(entity);
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(
                exception,
                "Competing or duplicate medical-service request detected for patient {PatientProfileId}.",
                patient.Id);
            return Result<MedicalServiceRequestDetailsDto>.Conflict(
                "This request conflicts with another active request. Refresh and try again.");
        }

        _logger.LogInformation(
            "Patient {PatientProfileId} created medical-service request {RequestId}.",
            patient.Id,
            entity.Id);

        return Result<MedicalServiceRequestDetailsDto>.Success(
            await LoadDetailsAsync(entity.Id, includePatientContact: false, ct),
            "Medical service request submitted successfully.");
    }

    public async Task<Result<PagedResult<MedicalServiceRequestSummaryDto>>> GetPatientRequestsAsync(
        string patientUserId,
        PatientMedicalServiceRequestFilter filter,
        CancellationToken ct = default)
    {
        var patientId = await ResolveActivePatientProfileIdAsync(patientUserId, ct);
        if (!patientId.HasValue)
        {
            return Result<PagedResult<MedicalServiceRequestSummaryDto>>.NotFound(
                "Patient profile not found for the current account.");
        }

        var query = _context.MedicalServiceRequests
            .AsNoTracking()
            .Where(request => request.PatientProfileId == patientId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(request =>
                request.RequestNumber.Contains(search) ||
                request.ServiceNameSnapshot.Contains(search) ||
                request.MedicalServiceProviderProfile!.BusinessName!.Contains(search));
        }
        if (filter.Status.HasValue)
        {
            query = query.Where(request => request.Status == filter.Status.Value);
        }
        if (filter.ProviderId.HasValue)
        {
            query = query.Where(request =>
                request.MedicalServiceProviderProfileId == filter.ProviderId.Value);
        }
        query = ApplyDateRange(query, filter.DateFrom, filter.DateTo);
        query = ApplyPatientSort(query, filter.SortBy, filter.SortDirection);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .Select(SummaryProjection())
            .ToListAsync(ct);

        return Result<PagedResult<MedicalServiceRequestSummaryDto>>.Success(
            PagedResult<MedicalServiceRequestSummaryDto>.Create(
                items, filter.Page, filter.PageSize, total),
            "Medical service requests retrieved successfully.");
    }

    public async Task<Result<MedicalServiceRequestDetailsDto>> GetPatientRequestByIdAsync(
        string patientUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var patientId = await ResolveActivePatientProfileIdAsync(patientUserId, ct);
        if (!patientId.HasValue ||
            !await _context.MedicalServiceRequests.AsNoTracking().AnyAsync(
                request => request.Id == requestId && request.PatientProfileId == patientId.Value,
                ct))
        {
            return Result<MedicalServiceRequestDetailsDto>.NotFound(
                "Medical service request not found.");
        }

        return Result<MedicalServiceRequestDetailsDto>.Success(
            await LoadDetailsAsync(requestId, includePatientContact: false, ct),
            "Medical service request retrieved successfully.");
    }

    public async Task<Result<MedicalServiceRequestDetailsDto>> CancelByPatientAsync(
        string patientUserId,
        Guid requestId,
        CancelMedicalServiceRequestRequest request,
        CancellationToken ct = default)
    {
        var patientId = await ResolveActivePatientProfileIdAsync(patientUserId, ct);
        if (!patientId.HasValue)
        {
            return Result<MedicalServiceRequestDetailsDto>.NotFound(
                "Medical service request not found.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var entity = await _context.MedicalServiceRequests
                .FirstOrDefaultAsync(
                    item => item.Id == requestId && item.PatientProfileId == patientId.Value,
                    ct);
            if (entity is null)
            {
                return Result<MedicalServiceRequestDetailsDto>.NotFound(
                    "Medical service request not found.");
            }

            if (entity.Status == MedicalServiceRequestStatus.Accepted &&
                entity.ScheduledAt.HasValue &&
                entity.ScheduledAt.Value <= GetEgyptNow())
            {
                return Result<MedicalServiceRequestDetailsDto>.Conflict(
                    "An accepted request cannot be cancelled after its scheduled time has started.");
            }

            var failure = ApplyTransition(
                entity,
                MedicalServiceRequestStatus.CancelledByPatient,
                patientUserId,
                request.CancellationReason,
                mutation: item =>
                {
                    item.CancellationReason = request.CancellationReason.Trim();
                    item.CancelledAt = DateTime.UtcNow;
                });
            if (failure is not null)
            {
                return failure;
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Result<MedicalServiceRequestDetailsDto>.Conflict(
                "This request was changed by another action. Refresh and try again.");
        }

        return Result<MedicalServiceRequestDetailsDto>.Success(
            await LoadDetailsAsync(requestId, includePatientContact: false, ct),
            "Medical service request cancelled successfully.");
    }

    // =============================================================== Provider

    public async Task<Result<PagedResult<MedicalServiceRequestSummaryDto>>> GetProviderRequestsAsync(
        string providerUserId,
        ProviderMedicalServiceRequestFilter filter,
        CancellationToken ct = default)
    {
        var providerId = await ResolveActiveProviderProfileIdAsync(providerUserId, ct);
        if (!providerId.HasValue)
        {
            return Result<PagedResult<MedicalServiceRequestSummaryDto>>.NotFound(
                "Medical service provider profile not found for the current account.");
        }

        var query = _context.MedicalServiceRequests
            .AsNoTracking()
            .Where(request => request.MedicalServiceProviderProfileId == providerId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(request =>
                request.RequestNumber.Contains(search) ||
                request.ServiceNameSnapshot.Contains(search) ||
                request.PatientProfile!.User!.FullName.Contains(search));
        }
        if (filter.Status.HasValue)
        {
            query = query.Where(request => request.Status == filter.Status.Value);
        }
        if (filter.ServiceId.HasValue)
        {
            query = query.Where(request => request.MedicalServiceOfferingId == filter.ServiceId);
        }
        if (filter.DeliveryMode.HasValue)
        {
            query = query.Where(request => request.DeliveryMode == filter.DeliveryMode);
        }
        query = ApplyDateRange(query, filter.DateFrom, filter.DateTo);
        query = ApplyProviderSort(query, filter.SortBy, filter.SortDirection);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .Select(SummaryProjection())
            .ToListAsync(ct);

        return Result<PagedResult<MedicalServiceRequestSummaryDto>>.Success(
            PagedResult<MedicalServiceRequestSummaryDto>.Create(
                items, filter.Page, filter.PageSize, total),
            "Provider requests retrieved successfully.");
    }

    public async Task<Result<MedicalServiceRequestDetailsDto>> GetProviderRequestByIdAsync(
        string providerUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var providerId = await ResolveActiveProviderProfileIdAsync(providerUserId, ct);
        if (!providerId.HasValue ||
            !await _context.MedicalServiceRequests.AsNoTracking().AnyAsync(
                request => request.Id == requestId &&
                           request.MedicalServiceProviderProfileId == providerId.Value,
                ct))
        {
            return Result<MedicalServiceRequestDetailsDto>.NotFound(
                "Medical service request not found.");
        }

        return Result<MedicalServiceRequestDetailsDto>.Success(
            await LoadDetailsAsync(requestId, includePatientContact: true, ct),
            "Medical service request retrieved successfully.");
    }

    public async Task<Result<MedicalServiceRequestDetailsDto>> AcceptAsync(
        string providerUserId,
        Guid requestId,
        AcceptMedicalServiceRequestRequest request,
        CancellationToken ct = default)
    {
        if (!TimeOnly.TryParse(request.ScheduledStartTime, CultureInfo.InvariantCulture, out var startTime))
        {
            return Result<MedicalServiceRequestDetailsDto>.Invalid(
                "Confirmed start time is invalid.");
        }

        var dateValidation = ValidateBookableDateTime(request.ScheduledDate, startTime);
        if (dateValidation is not null)
        {
            return Result<MedicalServiceRequestDetailsDto>.Invalid(dateValidation);
        }

        var providerId = await ResolveActiveProviderProfileIdAsync(providerUserId, ct);
        if (!providerId.HasValue)
        {
            return Result<MedicalServiceRequestDetailsDto>.NotFound(
                "Medical service request not found.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var entity = await _context.MedicalServiceRequests
                .Include(item => item.MedicalServiceOffering)
                .FirstOrDefaultAsync(
                    item => item.Id == requestId &&
                            item.MedicalServiceProviderProfileId == providerId.Value,
                    ct);
            if (entity is null)
            {
                return Result<MedicalServiceRequestDetailsDto>.NotFound(
                    "Medical service request not found.");
            }

            if (entity.MedicalServiceOffering?.MedicalServiceProviderProfileId != providerId.Value)
            {
                return Result<MedicalServiceRequestDetailsDto>.Conflict(
                    "The requested service no longer belongs to this provider.");
            }
            if (!SupportsDeliveryMode(
                    entity.MedicalServiceOffering.DeliveryModeAvailability,
                    entity.DeliveryMode))
            {
                return Result<MedicalServiceRequestDetailsDto>.Conflict(
                    "The service no longer supports the selected delivery mode.");
            }

            var hoursValidation = await ValidateWorkingHoursAsync(
                providerId.Value,
                request.ScheduledDate,
                startTime,
                entity.DurationMinutesSnapshot,
                ct);
            if (hoursValidation is not null)
            {
                return Result<MedicalServiceRequestDetailsDto>.Invalid(hoursValidation);
            }

            var scheduledAt = request.ScheduledDate.ToDateTime(
                startTime,
                DateTimeKind.Unspecified);
            var failure = ApplyTransition(
                entity,
                MedicalServiceRequestStatus.Accepted,
                providerUserId,
                request.ProviderResponseNote,
                mutation: item =>
                {
                    item.ScheduledAt = scheduledAt;
                    item.ProviderResponseNote = Normalise(request.ProviderResponseNote);
                });
            if (failure is not null)
            {
                return failure;
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Result<MedicalServiceRequestDetailsDto>.Conflict(
                "This request was already changed. Refresh and try again.");
        }

        return Result<MedicalServiceRequestDetailsDto>.Success(
            await LoadDetailsAsync(requestId, includePatientContact: true, ct),
            "Medical service request accepted successfully.");
    }

    public Task<Result<MedicalServiceRequestDetailsDto>> RejectAsync(
        string providerUserId,
        Guid requestId,
        RejectMedicalServiceRequestRequest request,
        CancellationToken ct = default) =>
        TransitionProviderRequestAsync(
            providerUserId,
            requestId,
            MedicalServiceRequestStatus.Rejected,
            request.RejectionReason,
            entity =>
            {
                entity.RejectionReason = request.RejectionReason.Trim();
                entity.ProviderResponseNote = Normalise(request.ProviderResponseNote);
            },
            "Medical service request rejected successfully.",
            ct);

    public Task<Result<MedicalServiceRequestDetailsDto>> CancelByProviderAsync(
        string providerUserId,
        Guid requestId,
        CancelMedicalServiceRequestRequest request,
        CancellationToken ct = default) =>
        TransitionProviderRequestAsync(
            providerUserId,
            requestId,
            MedicalServiceRequestStatus.CancelledByProvider,
            request.CancellationReason,
            entity =>
            {
                entity.CancellationReason = request.CancellationReason.Trim();
                entity.CancelledAt = DateTime.UtcNow;
            },
            "Medical service request cancelled successfully.",
            ct);

    public async Task<Result<MedicalServiceRequestDetailsDto>> CompleteAsync(
        string providerUserId,
        Guid requestId,
        CancellationToken ct = default)
    {
        var providerId = await ResolveActiveProviderProfileIdAsync(providerUserId, ct);
        if (!providerId.HasValue)
        {
            return Result<MedicalServiceRequestDetailsDto>.NotFound(
                "Medical service request not found.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var entity = await _context.MedicalServiceRequests.FirstOrDefaultAsync(
                item => item.Id == requestId &&
                        item.MedicalServiceProviderProfileId == providerId.Value,
                ct);
            if (entity is null)
            {
                return Result<MedicalServiceRequestDetailsDto>.NotFound(
                    "Medical service request not found.");
            }
            if (!entity.ScheduledAt.HasValue)
            {
                return Result<MedicalServiceRequestDetailsDto>.Conflict(
                    "This request has no confirmed schedule.");
            }
            if (entity.ScheduledAt.Value > GetEgyptNow())
            {
                return Result<MedicalServiceRequestDetailsDto>.Conflict(
                    "A request cannot be completed before its scheduled start time.");
            }

            var failure = ApplyTransition(
                entity,
                MedicalServiceRequestStatus.Completed,
                providerUserId,
                "Service completed.",
                mutation: item => item.CompletedAt = DateTime.UtcNow);
            if (failure is not null)
            {
                return failure;
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Result<MedicalServiceRequestDetailsDto>.Conflict(
                "This request was already changed. Refresh and try again.");
        }

        return Result<MedicalServiceRequestDetailsDto>.Success(
            await LoadDetailsAsync(requestId, includePatientContact: true, ct),
            "Medical service request marked as completed.");
    }

    public async Task<Result<MedicalServiceRequestDashboardSummaryDto>> GetProviderDashboardSummaryAsync(
        string providerUserId,
        CancellationToken ct = default)
    {
        var providerId = await ResolveActiveProviderProfileIdAsync(providerUserId, ct);
        if (!providerId.HasValue)
        {
            return Result<MedicalServiceRequestDashboardSummaryDto>.NotFound(
                "Medical service provider profile not found for the current account.");
        }

        var requests = _context.MedicalServiceRequests
            .AsNoTracking()
            .Where(request => request.MedicalServiceProviderProfileId == providerId.Value);
        var now = GetEgyptNow();

        var summary = new MedicalServiceRequestDashboardSummaryDto
        {
            PendingCount = await requests.CountAsync(
                request => request.Status == MedicalServiceRequestStatus.Pending,
                ct),
            AcceptedUpcomingCount = await requests.CountAsync(
                request => request.Status == MedicalServiceRequestStatus.Accepted &&
                           request.ScheduledAt >= now,
                ct),
            CompletedCount = await requests.CountAsync(
                request => request.Status == MedicalServiceRequestStatus.Completed,
                ct),
            CancelledOrRejectedCount = await requests.CountAsync(
                request => request.Status == MedicalServiceRequestStatus.Rejected ||
                           request.Status == MedicalServiceRequestStatus.CancelledByPatient ||
                           request.Status == MedicalServiceRequestStatus.CancelledByProvider,
                ct)
        };

        return Result<MedicalServiceRequestDashboardSummaryDto>.Success(
            summary,
            "Provider request summary retrieved successfully.");
    }

    // ================================================================ Helpers

    private async Task<Result<MedicalServiceRequestDetailsDto>> TransitionProviderRequestAsync(
        string providerUserId,
        Guid requestId,
        MedicalServiceRequestStatus target,
        string reason,
        Action<MedicalServiceRequest> mutation,
        string successMessage,
        CancellationToken ct)
    {
        var providerId = await ResolveActiveProviderProfileIdAsync(providerUserId, ct);
        if (!providerId.HasValue)
        {
            return Result<MedicalServiceRequestDetailsDto>.NotFound(
                "Medical service request not found.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var entity = await _context.MedicalServiceRequests.FirstOrDefaultAsync(
                item => item.Id == requestId &&
                        item.MedicalServiceProviderProfileId == providerId.Value,
                ct);
            if (entity is null)
            {
                return Result<MedicalServiceRequestDetailsDto>.NotFound(
                    "Medical service request not found.");
            }

            var failure = ApplyTransition(
                entity,
                target,
                providerUserId,
                reason,
                mutation);
            if (failure is not null)
            {
                return failure;
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Result<MedicalServiceRequestDetailsDto>.Conflict(
                "This request was already changed. Refresh and try again.");
        }

        return Result<MedicalServiceRequestDetailsDto>.Success(
            await LoadDetailsAsync(requestId, includePatientContact: true, ct),
            successMessage);
    }

    private Result<MedicalServiceRequestDetailsDto>? ApplyTransition(
        MedicalServiceRequest entity,
        MedicalServiceRequestStatus target,
        string changedByUserId,
        string? reason,
        Action<MedicalServiceRequest> mutation)
    {
        if (!MedicalServiceRequestTransitions.CanTransition(entity.Status, target))
        {
            return Result<MedicalServiceRequestDetailsDto>.Conflict(
                $"A {entity.Status} request cannot transition to {target}.");
        }

        var previous = entity.Status;
        var now = DateTime.UtcNow;
        mutation(entity);
        entity.Status = target;
        entity.UpdatedAt = now;
        // The request is an existing tracked aggregate here. Add the audit row explicitly
        // so EF does not infer Modified from its pre-generated Guid key.
        _context.MedicalServiceRequestStatusHistory.Add(CreateHistory(
            entity,
            previous,
            target,
            changedByUserId,
            Normalise(reason),
            now));
        return null;
    }

    private static MedicalServiceRequestStatusHistory CreateHistory(
        MedicalServiceRequest request,
        MedicalServiceRequestStatus? previousStatus,
        MedicalServiceRequestStatus newStatus,
        string? changedByUserId,
        string? reason,
        DateTime createdAt) =>
        new()
        {
            MedicalServiceRequest = request,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByApplicationUserId = changedByUserId,
            Reason = reason,
            CreatedAt = createdAt
        };

    private async Task<MedicalServiceRequestDetailsDto> LoadDetailsAsync(
        Guid requestId,
        bool includePatientContact,
        CancellationToken ct)
    {
        var row = await _context.MedicalServiceRequests
            .AsNoTracking()
            .Where(request => request.Id == requestId)
            .Select(request => new
            {
                request.Id,
                request.RequestNumber,
                request.MedicalServiceProviderProfileId,
                ProviderName = request.MedicalServiceProviderProfile!.BusinessName ?? string.Empty,
                ProviderType = request.MedicalServiceProviderProfile.ProviderType,
                ProviderPhone = request.MedicalServiceProviderProfile.PhoneNumber,
                ProviderAddress = request.MedicalServiceProviderProfile.Address,
                PatientName = request.PatientProfile!.User!.FullName,
                PatientPhone = request.PatientProfile.User.PhoneNumber,
                PatientUserId = request.PatientProfile.UserId,
                ProviderUserId = request.MedicalServiceProviderProfile.UserId,
                request.MedicalServiceOfferingId,
                request.ServiceNameSnapshot,
                request.CategoryNameSnapshot,
                request.DeliveryMode,
                request.RequestedDate,
                request.PreferredStartTime,
                request.ScheduledAt,
                request.PriceSnapshot,
                request.DurationMinutesSnapshot,
                request.Status,
                request.PatientNotes,
                request.HomeVisitAddress,
                request.ProviderResponseNote,
                request.RejectionReason,
                request.CancellationReason,
                request.CreatedAt,
                request.CompletedAt,
                request.CancelledAt
            })
            .FirstAsync(ct);

        var historyRows = await _context.MedicalServiceRequestStatusHistory
            .AsNoTracking()
            .Where(history => history.MedicalServiceRequestId == requestId)
            .OrderBy(history => history.CreatedAt)
            .Select(history => new
            {
                history.PreviousStatus,
                history.NewStatus,
                history.ChangedByApplicationUserId,
                history.Reason,
                history.CreatedAt
            })
            .ToListAsync(ct);

        return new MedicalServiceRequestDetailsDto
        {
            Id = row.Id,
            RequestNumber = row.RequestNumber,
            ProviderId = row.MedicalServiceProviderProfileId,
            ProviderName = row.ProviderName,
            ProviderTypeName = row.ProviderType?.ToString(),
            ProviderPhoneNumber = row.ProviderPhone,
            ProviderAddress = row.ProviderAddress,
            PatientName = row.PatientName,
            PatientPhoneNumber = includePatientContact ? row.PatientPhone : null,
            ServiceId = row.MedicalServiceOfferingId,
            ServiceName = row.ServiceNameSnapshot,
            CategoryName = row.CategoryNameSnapshot,
            DeliveryMode = row.DeliveryMode,
            DeliveryModeName = DeliveryModeLabel(row.DeliveryMode),
            RequestedDate = row.RequestedDate,
            PreferredStartTime = row.PreferredStartTime.ToString("HH:mm"),
            ScheduledAt = row.ScheduledAt,
            PriceSnapshot = row.PriceSnapshot,
            DurationMinutesSnapshot = row.DurationMinutesSnapshot,
            Status = row.Status,
            StatusName = row.Status.ToString(),
            PatientNotes = row.PatientNotes,
            HomeVisitAddress = row.HomeVisitAddress,
            ProviderResponseNote = row.ProviderResponseNote,
            RejectionReason = row.RejectionReason,
            CancellationReason = row.CancellationReason,
            CreatedAt = row.CreatedAt,
            CompletedAt = row.CompletedAt,
            CancelledAt = row.CancelledAt,
            StatusHistory = historyRows.Select(history =>
                new MedicalServiceRequestStatusHistoryDto
                {
                    PreviousStatus = history.PreviousStatus,
                    NewStatus = history.NewStatus,
                    NewStatusName = history.NewStatus.ToString(),
                    ActorLabel = history.ChangedByApplicationUserId == row.PatientUserId
                        ? "Patient"
                        : history.ChangedByApplicationUserId == row.ProviderUserId
                            ? "Medical Service Provider"
                            : "System",
                    Reason = history.Reason,
                    CreatedAt = history.CreatedAt
                }).ToList()
        };
    }

    private async Task<Guid?> ResolveActivePatientProfileIdAsync(
        string userId,
        CancellationToken ct) =>
        await _context.PatientProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId && profile.User!.IsActive)
            .Select(profile => (Guid?)profile.Id)
            .FirstOrDefaultAsync(ct);

    private async Task<Guid?> ResolveActiveProviderProfileIdAsync(
        string userId,
        CancellationToken ct) =>
        await _context.MedicalServiceProviderProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId && profile.User!.IsActive)
            .Select(profile => (Guid?)profile.Id)
            .FirstOrDefaultAsync(ct);

    private async Task<string?> ValidateWorkingHoursAsync(
        Guid providerId,
        DateOnly date,
        TimeOnly start,
        int? durationMinutes,
        CancellationToken ct)
    {
        var hours = await _context.MedicalServiceProviderWorkingHours
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.MedicalServiceProviderProfileId == providerId &&
                        item.DayOfWeek == date.DayOfWeek,
                ct);

        if (hours is null || hours.IsClosed || !hours.OpenTime.HasValue || !hours.CloseTime.HasValue)
        {
            return "The provider is closed on the selected day.";
        }

        if (start < hours.OpenTime.Value || start >= hours.CloseTime.Value)
        {
            return "The selected time is outside the provider's working hours.";
        }

        var end = start.AddMinutes(durationMinutes ?? 0);
        if (end > hours.CloseTime.Value || (durationMinutes > 0 && end <= start))
        {
            return "The selected service would finish after the provider closes.";
        }

        return null;
    }

    private async Task<bool> ActiveDuplicateExistsAsync(
        Guid patientProfileId,
        Guid serviceOfferingId,
        DateOnly requestedDate,
        TimeOnly preferredStartTime,
        CancellationToken ct) =>
        await _context.MedicalServiceRequests
            .AsNoTracking()
            .AnyAsync(request =>
                    request.PatientProfileId == patientProfileId &&
                    request.MedicalServiceOfferingId == serviceOfferingId &&
                    request.RequestedDate == requestedDate &&
                    request.PreferredStartTime == preferredStartTime &&
                    (request.Status == MedicalServiceRequestStatus.Pending ||
                     request.Status == MedicalServiceRequestStatus.Accepted),
                ct);

    private async Task<string> GenerateRequestNumberAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
            var candidate = $"MSR-{DateTime.UtcNow.Year}-{suffix}";
            if (!await _context.MedicalServiceRequests.AsNoTracking()
                    .AnyAsync(request => request.RequestNumber == candidate, ct))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not generate a unique medical service request number.");
    }

    private static string? ValidateBookableDateTime(DateOnly date, TimeOnly start)
    {
        var now = GetEgyptNow();
        var today = DateOnly.FromDateTime(now);
        if (date < today)
        {
            return "The selected date is in the past.";
        }
        if (date > today.AddDays(MedicalServiceRequestLimits.MaximumBookingDays))
        {
            return $"Requests can be scheduled at most {MedicalServiceRequestLimits.MaximumBookingDays} days ahead.";
        }
        if (date == today && start <= TimeOnly.FromDateTime(now))
        {
            return "The selected time has already passed.";
        }
        return null;
    }

    private static bool SupportsDeliveryMode(
        ServiceDeliveryModeAvailability availability,
        ServiceDeliveryMode selected) =>
        availability switch
        {
            ServiceDeliveryModeAvailability.AtProviderLocationOnly =>
                selected == ServiceDeliveryMode.AtProviderLocation,
            ServiceDeliveryModeAvailability.HomeVisitOnly =>
                selected == ServiceDeliveryMode.HomeVisit,
            ServiceDeliveryModeAvailability.Both => true,
            _ => false
        };

    private static IQueryable<MedicalServiceRequest> ApplyDateRange(
        IQueryable<MedicalServiceRequest> query,
        DateOnly? from,
        DateOnly? to)
    {
        if (from.HasValue)
        {
            query = query.Where(request => request.RequestedDate >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(request => request.RequestedDate <= to.Value);
        }
        return query;
    }

    private static IQueryable<MedicalServiceRequest> ApplyPatientSort(
        IQueryable<MedicalServiceRequest> query,
        string sortBy,
        string direction)
    {
        var descending = !string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase);
        return sortBy.Trim().ToLowerInvariant() switch
        {
            "requesteddate" => descending
                ? query.OrderByDescending(request => request.RequestedDate)
                    .ThenByDescending(request => request.PreferredStartTime)
                : query.OrderBy(request => request.RequestedDate)
                    .ThenBy(request => request.PreferredStartTime),
            "status" => descending
                ? query.OrderByDescending(request => request.Status)
                : query.OrderBy(request => request.Status),
            _ => descending
                ? query.OrderByDescending(request => request.CreatedAt)
                : query.OrderBy(request => request.CreatedAt)
        };
    }

    private static IQueryable<MedicalServiceRequest> ApplyProviderSort(
        IQueryable<MedicalServiceRequest> query,
        string sortBy,
        string direction)
    {
        var descending = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.Trim().ToLowerInvariant() switch
        {
            "createdat" => descending
                ? query.OrderByDescending(request => request.CreatedAt)
                : query.OrderBy(request => request.CreatedAt),
            "requesteddate" => descending
                ? query.OrderByDescending(request => request.RequestedDate)
                    .ThenByDescending(request => request.PreferredStartTime)
                : query.OrderBy(request => request.RequestedDate)
                    .ThenBy(request => request.PreferredStartTime),
            _ => query
                .OrderBy(request => request.Status == MedicalServiceRequestStatus.Pending ? 0 : 1)
                .ThenBy(request => request.RequestedDate)
                .ThenBy(request => request.PreferredStartTime)
                .ThenByDescending(request => request.CreatedAt)
        };
    }

    private static Expression<Func<MedicalServiceRequest, MedicalServiceRequestSummaryDto>>
        SummaryProjection() =>
        request => new MedicalServiceRequestSummaryDto
        {
            Id = request.Id,
            RequestNumber = request.RequestNumber,
            ProviderId = request.MedicalServiceProviderProfileId,
            ProviderName = request.MedicalServiceProviderProfile!.BusinessName ?? string.Empty,
            PatientName = request.PatientProfile!.User!.FullName,
            ServiceId = request.MedicalServiceOfferingId,
            ServiceName = request.ServiceNameSnapshot,
            CategoryName = request.CategoryNameSnapshot,
            DeliveryMode = request.DeliveryMode,
            DeliveryModeName = request.DeliveryMode.ToString(),
            RequestedDate = request.RequestedDate,
            PreferredStartTime = request.PreferredStartTime.ToString("HH:mm"),
            ScheduledAt = request.ScheduledAt,
            PriceSnapshot = request.PriceSnapshot,
            Status = request.Status,
            StatusName = request.Status.ToString(),
            CreatedAt = request.CreatedAt
        };

    private static string DeliveryModeLabel(ServiceDeliveryMode mode) =>
        mode == ServiceDeliveryMode.HomeVisit ? "Home visit" : "At provider location";

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime GetEgyptNow() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EgyptTimeZone);

    private static TimeZoneInfo ResolveEgyptTimeZone()
    {
        foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the platform-specific alternative.
            }
        }

        return TimeZoneInfo.Utc;
    }
}
