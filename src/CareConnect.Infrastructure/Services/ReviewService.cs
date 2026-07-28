using System.Data;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Reviews;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

public sealed class ReviewService : IReviewService, IRatingQueryService, IReviewModerationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notifications;

    public ReviewService(
        ApplicationDbContext context,
        INotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    public async Task<Result<ReviewEligibilityDto>> GetEligibilityAsync(
        string userId, ReviewType type, Guid sourceId, CancellationToken ct = default)
    {
        var patientId = await ResolveActivePatientIdAsync(userId, ct);
        if (!patientId.HasValue)
            return Result<ReviewEligibilityDto>.NotFound("Completed interaction not found.");

        var sourceExists = await CompletedSourceExistsAsync(patientId.Value, type, sourceId, ct);
        if (!sourceExists)
            return Result<ReviewEligibilityDto>.NotFound("Completed interaction not found.");

        var reviewId = await ExistingReviewIdAsync(patientId.Value, type, sourceId, ct);
        return Result<ReviewEligibilityDto>.Success(new ReviewEligibilityDto
        {
            IsEligible = true,
            HasReview = reviewId.HasValue,
            ReviewId = reviewId,
            Message = reviewId.HasValue
                ? "A verified review already exists and may be edited."
                : "This completed interaction is eligible for a verified review."
        });
    }

    public async Task<Result<ReviewDto>> GetPatientReviewAsync(
        string userId, ReviewType type, Guid sourceId, CancellationToken ct = default)
    {
        var patientId = await ResolveActivePatientIdAsync(userId, ct);
        if (!patientId.HasValue)
            return Result<ReviewDto>.NotFound("Review not found.");

        var row = await Rows()
            .Where(r => r.PatientProfileId == patientId.Value &&
                        r.ReviewType == type && r.SourceId == sourceId)
            .FirstOrDefaultAsync(ct);
        return row is null
            ? Result<ReviewDto>.NotFound("Review not found.")
            : Result<ReviewDto>.Success(ToDto(row, includeModerationReason: false));
    }

    public async Task<Result<ReviewDto>> CreateAsync(
        string userId, ReviewType type, Guid sourceId, SaveReviewRequest request,
        CancellationToken ct = default)
    {
        var patientId = await ResolveActivePatientIdAsync(userId, ct);
        if (!patientId.HasValue)
            return Result<ReviewDto>.NotFound("Completed interaction not found.");

        var target = await ResolveCompletedTargetAsync(patientId.Value, type, sourceId, ct);
        if (!target.HasValue)
            return Result<ReviewDto>.NotFound("Completed interaction not found.");

        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, ct);
        try
        {
            if (await ExistingReviewIdAsync(patientId.Value, type, sourceId, ct) is not null)
                return Result<ReviewDto>.Conflict("A review already exists for this completed interaction.");

            var now = DateTime.UtcNow;
            var comment = NormalizeComment(request.Comment);
            var reviewId = Guid.NewGuid();
            switch (type)
            {
                case ReviewType.Doctor:
                    _context.AppointmentDoctorReviews.Add(new AppointmentDoctorReview
                    {
                        Id = reviewId,
                        AppointmentId = sourceId, PatientProfileId = patientId.Value,
                        DoctorProfileId = target.Value, Rating = request.Rating,
                        Comment = comment, CreatedAt = now
                    });
                    break;
                case ReviewType.Hospital:
                    _context.AppointmentHospitalReviews.Add(new AppointmentHospitalReview
                    {
                        Id = reviewId,
                        AppointmentId = sourceId, PatientProfileId = patientId.Value,
                        HospitalProfileId = target.Value, Rating = request.Rating,
                        Comment = comment, CreatedAt = now
                    });
                    break;
                case ReviewType.MedicalServiceProvider:
                    _context.MedicalServiceProviderReviews.Add(new MedicalServiceProviderReview
                    {
                        Id = reviewId,
                        MedicalServiceRequestId = sourceId, PatientProfileId = patientId.Value,
                        MedicalServiceProviderProfileId = target.Value, Rating = request.Rating,
                        Comment = comment, CreatedAt = now
                    });
                    break;
                default:
                    return Result<ReviewDto>.Invalid("Review type is invalid.");
            }

            var ownerUserId = await ResolveReviewOwnerUserIdAsync(type, target.Value, ct);
            if (ownerUserId is not null)
            {
                var (route, reviewKind) = type switch
                {
                    ReviewType.Doctor => ("/dashboard/doctor/reviews", "doctor"),
                    ReviewType.Hospital => ("/dashboard/hospital/reviews", "hospital"),
                    ReviewType.MedicalServiceProvider =>
                        ("/dashboard/service-provider/reviews", "medical service"),
                    _ => ("/notifications", "service")
                };
                await _notifications.QueueAsync(
                    WorkflowNotificationFactory.NewReview(
                        reviewId,
                        ownerUserId,
                        route,
                        reviewKind),
                    ct);
            }
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            return Result<ReviewDto>.Conflict("A review already exists for this completed interaction.");
        }

        return await GetPatientReviewAsync(userId, type, sourceId, ct);
    }

    public async Task<Result<ReviewDto>> UpdateAsync(
        string userId, ReviewType type, Guid sourceId, SaveReviewRequest request,
        CancellationToken ct = default)
    {
        var patientId = await ResolveActivePatientIdAsync(userId, ct);
        if (!patientId.HasValue ||
            !await CompletedSourceExistsAsync(patientId.Value, type, sourceId, ct))
            return Result<ReviewDto>.NotFound("Review not found.");

        var comment = NormalizeComment(request.Comment);
        var now = DateTime.UtcNow;
        var updated = type switch
        {
            ReviewType.Doctor => await _context.AppointmentDoctorReviews
                .Where(r => r.AppointmentId == sourceId && r.PatientProfileId == patientId.Value)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Rating, request.Rating)
                    .SetProperty(r => r.Comment, comment).SetProperty(r => r.UpdatedAt, now), ct),
            ReviewType.Hospital => await _context.AppointmentHospitalReviews
                .Where(r => r.AppointmentId == sourceId && r.PatientProfileId == patientId.Value)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Rating, request.Rating)
                    .SetProperty(r => r.Comment, comment).SetProperty(r => r.UpdatedAt, now), ct),
            ReviewType.MedicalServiceProvider => await _context.MedicalServiceProviderReviews
                .Where(r => r.MedicalServiceRequestId == sourceId && r.PatientProfileId == patientId.Value)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Rating, request.Rating)
                    .SetProperty(r => r.Comment, comment).SetProperty(r => r.UpdatedAt, now), ct),
            _ => 0
        };

        if (updated == 0) return Result<ReviewDto>.NotFound("Review not found.");
        return await GetPatientReviewAsync(userId, type, sourceId, ct);
    }

    public async Task<Result<PagedResult<ReviewDto>>> GetPatientReviewsAsync(
        string userId, PatientReviewFilter filter, CancellationToken ct = default)
    {
        var patientId = await ResolveActivePatientIdAsync(userId, ct);
        if (!patientId.HasValue)
            return Result<PagedResult<ReviewDto>>.NotFound("Patient profile not found.");

        var query = Rows().Where(r => r.PatientProfileId == patientId.Value);
        if (filter.ReviewType.HasValue) query = query.Where(r => r.ReviewType == filter.ReviewType.Value);
        if (filter.Rating.HasValue) query = query.Where(r => r.Rating == filter.Rating.Value);
        if (filter.ModerationStatus.HasValue)
            query = query.Where(r => r.ModerationStatus == filter.ModerationStatus.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(r => r.TargetName.Contains(term) ||
                                     (r.Comment != null && r.Comment.Contains(term)) ||
                                     r.SourceReference.Contains(term));
        }
        return Result<PagedResult<ReviewDto>>.Success(
            await PageAsync(query, filter.Page, filter.PageSize, filter.SortBy, false, ct));
    }

    public async Task<Result<PagedResult<ReviewDto>>> GetPublicReviewsAsync(
        ReviewType type, Guid targetId, ReviewListFilter filter, CancellationToken ct = default)
    {
        if (!await PublicTargetExistsAsync(type, targetId, ct))
            return Result<PagedResult<ReviewDto>>.NotFound("Reviewed profile not found.");

        var query = Rows().Where(r => r.ReviewType == type && r.TargetId == targetId &&
                                      r.ModerationStatus == ReviewModerationStatus.Visible);
        query = ApplyListFilters(query, filter);
        return Result<PagedResult<ReviewDto>>.Success(
            await PageAsync(query, filter.Page, filter.PageSize, filter.SortBy, false, ct));
    }

    public async Task<Result<RatingSummaryDto>> GetPublicSummaryAsync(
        ReviewType type, Guid targetId, CancellationToken ct = default)
    {
        if (!await PublicTargetExistsAsync(type, targetId, ct))
            return Result<RatingSummaryDto>.NotFound("Reviewed profile not found.");
        return Result<RatingSummaryDto>.Success(
            await BuildSummaryAsync(type, targetId, ct), "Rating summary retrieved successfully.");
    }

    public async Task<Result<PagedResult<ReviewDto>>> GetOwnerReviewsAsync(
        string userId, ReviewType type, ReviewListFilter filter, CancellationToken ct = default)
    {
        var targetId = await ResolveOwnerTargetIdAsync(userId, type, ct);
        if (!targetId.HasValue)
            return Result<PagedResult<ReviewDto>>.NotFound("Reviewed profile not found.");
        var query = Rows().Where(r => r.ReviewType == type && r.TargetId == targetId.Value &&
                                      r.ModerationStatus == ReviewModerationStatus.Visible);
        query = ApplyListFilters(query, filter);
        return Result<PagedResult<ReviewDto>>.Success(
            await PageAsync(query, filter.Page, filter.PageSize, filter.SortBy, false, ct));
    }

    public async Task<Result<RatingSummaryDto>> GetOwnerSummaryAsync(
        string userId, ReviewType type, CancellationToken ct = default)
    {
        var targetId = await ResolveOwnerTargetIdAsync(userId, type, ct);
        return targetId.HasValue
            ? Result<RatingSummaryDto>.Success(await BuildSummaryAsync(type, targetId.Value, ct))
            : Result<RatingSummaryDto>.NotFound("Reviewed profile not found.");
    }

    public async Task<Result<PagedResult<ReviewDto>>> GetAllAsync(
        string adminUserId, SuperAdminReviewFilter filter, CancellationToken ct = default)
    {
        if (!await IsActiveUserAsync(adminUserId, ct))
            return Result<PagedResult<ReviewDto>>.Failure(
                ResultStatus.Forbidden, "Your account is inactive.");

        var query = Rows();
        if (filter.ReviewType.HasValue) query = query.Where(r => r.ReviewType == filter.ReviewType.Value);
        if (filter.ModerationStatus.HasValue)
            query = query.Where(r => r.ModerationStatus == filter.ModerationStatus.Value);
        if (filter.Rating.HasValue) query = query.Where(r => r.Rating == filter.Rating.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(r => r.TargetName.Contains(term) || r.PatientFullName.Contains(term) ||
                                     (r.Comment != null && r.Comment.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(filter.PatientName))
            query = query.Where(r => r.PatientFullName.Contains(filter.PatientName.Trim()));
        if (!string.IsNullOrWhiteSpace(filter.TargetName))
            query = query.Where(r => r.TargetName.Contains(filter.TargetName.Trim()));
        if (filter.DateFrom.HasValue)
            query = query.Where(r => r.CreatedAt >= filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (filter.DateTo.HasValue)
            query = query.Where(r => r.CreatedAt < filter.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        return Result<PagedResult<ReviewDto>>.Success(
            await PageAsync(query, filter.Page, filter.PageSize, filter.SortBy, true, ct));
    }

    public async Task<Result<ReviewDto>> GetByIdAsync(
        string adminUserId, ReviewType type, Guid id, CancellationToken ct = default)
    {
        if (!await IsActiveUserAsync(adminUserId, ct))
            return Result<ReviewDto>.Failure(ResultStatus.Forbidden, "Your account is inactive.");

        var row = await Rows().FirstOrDefaultAsync(r => r.ReviewType == type && r.Id == id, ct);
        return row is null
            ? Result<ReviewDto>.NotFound("Review not found.")
            : Result<ReviewDto>.Success(ToDto(row, true));
    }

    public Task<Result<ReviewDto>> HideAsync(
        string adminUserId, ReviewType type, Guid id, ModerateReviewRequest request,
        CancellationToken ct = default) =>
        ModerateAsync(adminUserId, type, id, ReviewModerationStatus.Hidden, request.Reason, ct);

    public Task<Result<ReviewDto>> RestoreAsync(
        string adminUserId, ReviewType type, Guid id, CancellationToken ct = default) =>
        ModerateAsync(adminUserId, type, id, ReviewModerationStatus.Visible, null, ct);

    private async Task<Result<ReviewDto>> ModerateAsync(
        string adminUserId, ReviewType type, Guid id, ReviewModerationStatus status,
        string? reason, CancellationToken ct)
    {
        if (!await IsActiveUserAsync(adminUserId, ct))
            return Result<ReviewDto>.Failure(ResultStatus.Forbidden, "Your account is inactive.");

        var now = DateTime.UtcNow;
        var normalizedReason = status == ReviewModerationStatus.Hidden ? reason?.Trim() : null;
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            ct);

        var patientUserId = await ResolveReviewPatientUserIdAsync(type, id, ct);
        if (patientUserId is null)
        {
            return Result<ReviewDto>.NotFound("Review not found.");
        }

        var updated = type switch
        {
            ReviewType.Doctor => await UpdateModerationAsync(
                _context.AppointmentDoctorReviews.Where(r => r.Id == id),
                status, normalizedReason, adminUserId, now, ct),
            ReviewType.Hospital => await UpdateModerationAsync(
                _context.AppointmentHospitalReviews.Where(r => r.Id == id),
                status, normalizedReason, adminUserId, now, ct),
            ReviewType.MedicalServiceProvider => await UpdateModerationAsync(
                _context.MedicalServiceProviderReviews.Where(r => r.Id == id),
                status, normalizedReason, adminUserId, now, ct),
            _ => 0
        };
        if (updated == 0)
        {
            return Result<ReviewDto>.NotFound("Review not found.");
        }

        var hidden = status == ReviewModerationStatus.Hidden;
        await _notifications.QueueAsync(
            WorkflowNotificationFactory.ReviewModerated(
                id,
                patientUserId,
                hidden ? "hidden" : "restored",
                hidden ? "Review hidden by moderation" : "Review restored",
                hidden ? NotificationType.Warning : NotificationType.Success),
            ct);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await GetByIdAsync(adminUserId, type, id, ct);
    }

    private static Task<int> UpdateModerationAsync<TEntity>(
        IQueryable<TEntity> query, ReviewModerationStatus status, string? reason,
        string adminUserId, DateTime now, CancellationToken ct)
        where TEntity : class, IVerifiedReview =>
        query.ExecuteUpdateAsync(setters => setters
            .SetProperty(r => r.ModerationStatus, status)
            .SetProperty(r => r.ModerationReason, reason)
            .SetProperty(r => r.ModeratedByApplicationUserId, adminUserId)
            .SetProperty(r => r.ModeratedAt, now), ct);

    private Task<string?> ResolveReviewOwnerUserIdAsync(
        ReviewType type,
        Guid targetId,
        CancellationToken ct) =>
        type switch
        {
            ReviewType.Doctor => _context.DoctorProfiles
                .Where(profile => profile.Id == targetId)
                .Select(profile => profile.UserId)
                .FirstOrDefaultAsync(ct),
            ReviewType.Hospital => _context.HospitalProfiles
                .Where(profile => profile.Id == targetId)
                .Select(profile => profile.UserId)
                .FirstOrDefaultAsync(ct),
            ReviewType.MedicalServiceProvider => _context.MedicalServiceProviderProfiles
                .Where(profile => profile.Id == targetId)
                .Select(profile => profile.UserId)
                .FirstOrDefaultAsync(ct),
            _ => Task.FromResult<string?>(null)
        };

    private Task<string?> ResolveReviewPatientUserIdAsync(
        ReviewType type,
        Guid reviewId,
        CancellationToken ct) =>
        type switch
        {
            ReviewType.Doctor => _context.AppointmentDoctorReviews
                .Where(review => review.Id == reviewId)
                .Select(review => review.PatientProfile!.UserId)
                .FirstOrDefaultAsync(ct),
            ReviewType.Hospital => _context.AppointmentHospitalReviews
                .Where(review => review.Id == reviewId)
                .Select(review => review.PatientProfile!.UserId)
                .FirstOrDefaultAsync(ct),
            ReviewType.MedicalServiceProvider => _context.MedicalServiceProviderReviews
                .Where(review => review.Id == reviewId)
                .Select(review => review.PatientProfile!.UserId)
                .FirstOrDefaultAsync(ct),
            _ => Task.FromResult<string?>(null)
        };

    private IQueryable<ReviewRow> Rows()
    {
        var doctors = _context.AppointmentDoctorReviews.AsNoTracking().Select(r => new ReviewRow
        {
            Id = r.Id, ReviewType = ReviewType.Doctor, SourceId = r.AppointmentId,
            SourceReference = "Appointment", TargetId = r.DoctorProfileId,
            TargetName = r.DoctorProfile!.User!.FullName, PatientProfileId = r.PatientProfileId,
            PatientFullName = r.PatientProfile!.User!.FullName, Rating = r.Rating, Comment = r.Comment,
            ModerationStatus = r.ModerationStatus, ModerationReason = r.ModerationReason,
            ModeratedAt = r.ModeratedAt, CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt
        });
        var hospitals = _context.AppointmentHospitalReviews.AsNoTracking().Select(r => new ReviewRow
        {
            Id = r.Id, ReviewType = ReviewType.Hospital, SourceId = r.AppointmentId,
            SourceReference = "Appointment", TargetId = r.HospitalProfileId,
            TargetName = r.HospitalProfile!.HospitalName ?? string.Empty,
            PatientProfileId = r.PatientProfileId, PatientFullName = r.PatientProfile!.User!.FullName,
            Rating = r.Rating, Comment = r.Comment, ModerationStatus = r.ModerationStatus,
            ModerationReason = r.ModerationReason, ModeratedAt = r.ModeratedAt,
            CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt
        });
        var providers = _context.MedicalServiceProviderReviews.AsNoTracking().Select(r => new ReviewRow
        {
            Id = r.Id, ReviewType = ReviewType.MedicalServiceProvider,
            SourceId = r.MedicalServiceRequestId,
            SourceReference = r.MedicalServiceRequest!.RequestNumber,
            TargetId = r.MedicalServiceProviderProfileId,
            TargetName = r.MedicalServiceProviderProfile!.BusinessName ?? string.Empty,
            PatientProfileId = r.PatientProfileId, PatientFullName = r.PatientProfile!.User!.FullName,
            Rating = r.Rating, Comment = r.Comment, ModerationStatus = r.ModerationStatus,
            ModerationReason = r.ModerationReason, ModeratedAt = r.ModeratedAt,
            CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt
        });
        return doctors.Concat(hospitals).Concat(providers);
    }

    private static IQueryable<ReviewRow> ApplyListFilters(
        IQueryable<ReviewRow> query, ReviewListFilter filter)
    {
        if (filter.Rating.HasValue) query = query.Where(r => r.Rating == filter.Rating.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(r => (r.Comment != null && r.Comment.Contains(term)) ||
                                     r.PatientFullName.Contains(term));
        }
        if (filter.DateFrom.HasValue)
            query = query.Where(r => r.CreatedAt >= filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (filter.DateTo.HasValue)
            query = query.Where(r => r.CreatedAt < filter.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        return query;
    }

    private static async Task<PagedResult<ReviewDto>> PageAsync(
        IQueryable<ReviewRow> query, int page, int pageSize, string sortBy,
        bool includeModerationReason, CancellationToken ct)
    {
        query = sortBy switch
        {
            "oldest" => query.OrderBy(r => r.CreatedAt),
            "highest-rating" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            "lowest-rating" => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };
        var total = await query.CountAsync(ct);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return PagedResult<ReviewDto>.Create(
            rows.Select(r => ToDto(r, includeModerationReason)).ToList(), page, pageSize, total);
    }

    private async Task<RatingSummaryDto> BuildSummaryAsync(
        ReviewType type, Guid targetId, CancellationToken ct)
    {
        IQueryable<int> ratings = type switch
        {
            ReviewType.Doctor => _context.AppointmentDoctorReviews.AsNoTracking()
                .Where(r => r.DoctorProfileId == targetId &&
                            r.ModerationStatus == ReviewModerationStatus.Visible).Select(r => r.Rating),
            ReviewType.Hospital => _context.AppointmentHospitalReviews.AsNoTracking()
                .Where(r => r.HospitalProfileId == targetId &&
                            r.ModerationStatus == ReviewModerationStatus.Visible).Select(r => r.Rating),
            ReviewType.MedicalServiceProvider => _context.MedicalServiceProviderReviews.AsNoTracking()
                .Where(r => r.MedicalServiceProviderProfileId == targetId &&
                            r.ModerationStatus == ReviewModerationStatus.Visible).Select(r => r.Rating),
            _ => Enumerable.Empty<int>().AsQueryable()
        };
        var grouped = await ratings.GroupBy(r => r).Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var count = grouped.Sum(g => g.Count);
        return new RatingSummaryDto
        {
            ReviewCount = count,
            AverageRating = count == 0 ? null : Math.Round(grouped.Sum(g => g.Rating * g.Count) / (double)count, 1),
            Distribution = new RatingDistributionDto
            {
                OneStar = grouped.FirstOrDefault(g => g.Rating == 1)?.Count ?? 0,
                TwoStars = grouped.FirstOrDefault(g => g.Rating == 2)?.Count ?? 0,
                ThreeStars = grouped.FirstOrDefault(g => g.Rating == 3)?.Count ?? 0,
                FourStars = grouped.FirstOrDefault(g => g.Rating == 4)?.Count ?? 0,
                FiveStars = grouped.FirstOrDefault(g => g.Rating == 5)?.Count ?? 0
            }
        };
    }

    private async Task<Guid?> ResolveActivePatientIdAsync(string userId, CancellationToken ct) =>
        await _context.PatientProfiles.AsNoTracking()
            .Where(p => p.UserId == userId && p.User!.IsActive).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);

    private Task<bool> IsActiveUserAsync(string userId, CancellationToken ct) =>
        _context.Users.AsNoTracking().AnyAsync(user => user.Id == userId && user.IsActive, ct);

    private async Task<Guid?> ResolveOwnerTargetIdAsync(
        string userId, ReviewType type, CancellationToken ct) =>
        type switch
        {
            ReviewType.Doctor => await _context.DoctorProfiles.AsNoTracking()
                .Where(p => p.UserId == userId && p.User!.IsActive).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct),
            ReviewType.Hospital => await _context.HospitalProfiles.AsNoTracking()
                .Where(p => p.UserId == userId && p.User!.IsActive).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct),
            ReviewType.MedicalServiceProvider => await _context.MedicalServiceProviderProfiles.AsNoTracking()
                .Where(p => p.UserId == userId && p.User!.IsActive).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct),
            _ => null
        };

    private Task<bool> PublicTargetExistsAsync(ReviewType type, Guid id, CancellationToken ct) =>
        type switch
        {
            ReviewType.Doctor => _context.DoctorProfiles.AsNoTracking()
                .AnyAsync(p => p.Id == id && p.User!.IsActive && p.IsProfileCompleted, ct),
            ReviewType.Hospital => _context.HospitalProfiles.AsNoTracking()
                .AnyAsync(p => p.Id == id && p.User!.IsActive && p.IsProfileCompleted, ct),
            ReviewType.MedicalServiceProvider => _context.MedicalServiceProviderProfiles.AsNoTracking()
                .AnyAsync(p => p.Id == id && p.User!.IsActive && p.IsPublished, ct),
            _ => Task.FromResult(false)
        };

    private Task<bool> CompletedSourceExistsAsync(
        Guid patientId, ReviewType type, Guid sourceId, CancellationToken ct) =>
        type switch
        {
            ReviewType.Doctor or ReviewType.Hospital => _context.Appointments.AsNoTracking()
                .AnyAsync(a => a.Id == sourceId && a.PatientProfileId == patientId &&
                               a.Status == AppointmentStatus.Completed, ct),
            ReviewType.MedicalServiceProvider => _context.MedicalServiceRequests.AsNoTracking()
                .AnyAsync(r => r.Id == sourceId && r.PatientProfileId == patientId &&
                               r.Status == MedicalServiceRequestStatus.Completed, ct),
            _ => Task.FromResult(false)
        };

    private async Task<Guid?> ResolveCompletedTargetAsync(
        Guid patientId, ReviewType type, Guid sourceId, CancellationToken ct) =>
        type switch
        {
            ReviewType.Doctor => await _context.Appointments.AsNoTracking()
                .Where(a => a.Id == sourceId && a.PatientProfileId == patientId &&
                            a.Status == AppointmentStatus.Completed)
                .Select(a => (Guid?)a.DoctorProfileId).FirstOrDefaultAsync(ct),
            ReviewType.Hospital => await _context.Appointments.AsNoTracking()
                .Where(a => a.Id == sourceId && a.PatientProfileId == patientId &&
                            a.Status == AppointmentStatus.Completed)
                .Select(a => (Guid?)a.HospitalProfileId).FirstOrDefaultAsync(ct),
            ReviewType.MedicalServiceProvider => await _context.MedicalServiceRequests.AsNoTracking()
                .Where(r => r.Id == sourceId && r.PatientProfileId == patientId &&
                            r.Status == MedicalServiceRequestStatus.Completed)
                .Select(r => (Guid?)r.MedicalServiceProviderProfileId).FirstOrDefaultAsync(ct),
            _ => null
        };

    private async Task<Guid?> ExistingReviewIdAsync(
        Guid patientId, ReviewType type, Guid sourceId, CancellationToken ct) =>
        type switch
        {
            ReviewType.Doctor => await _context.AppointmentDoctorReviews.AsNoTracking()
                .Where(r => r.AppointmentId == sourceId && r.PatientProfileId == patientId)
                .Select(r => (Guid?)r.Id).FirstOrDefaultAsync(ct),
            ReviewType.Hospital => await _context.AppointmentHospitalReviews.AsNoTracking()
                .Where(r => r.AppointmentId == sourceId && r.PatientProfileId == patientId)
                .Select(r => (Guid?)r.Id).FirstOrDefaultAsync(ct),
            ReviewType.MedicalServiceProvider => await _context.MedicalServiceProviderReviews.AsNoTracking()
                .Where(r => r.MedicalServiceRequestId == sourceId && r.PatientProfileId == patientId)
                .Select(r => (Guid?)r.Id).FirstOrDefaultAsync(ct),
            _ => null
        };

    private static string? NormalizeComment(string? comment) =>
        string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

    private static ReviewDto ToDto(ReviewRow row, bool includeModerationReason) => new()
    {
        Id = row.Id, ReviewType = row.ReviewType, ReviewTypeName = TypeName(row.ReviewType),
        SourceId = row.SourceId, SourceReference = row.SourceReference,
        TargetId = row.TargetId, TargetName = row.TargetName,
        PatientDisplayName = SafePatientName(row.PatientFullName), Rating = row.Rating,
        Comment = row.Comment, ModerationStatus = row.ModerationStatus,
        ModerationStatusName = row.ModerationStatus.ToString(),
        ModerationReason = includeModerationReason ? row.ModerationReason : null,
        ModeratedAt = includeModerationReason ? row.ModeratedAt : null,
        CreatedAt = row.CreatedAt, UpdatedAt = row.UpdatedAt
    };

    private static string SafePatientName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "Verified Patient",
            1 => parts[0],
            _ => $"{parts[0]} {char.ToUpperInvariant(parts[^1][0])}."
        };
    }

    private static string TypeName(ReviewType type) => type switch
    {
        ReviewType.Doctor => "Doctor",
        ReviewType.Hospital => "Hospital",
        ReviewType.MedicalServiceProvider => "Medical Service Provider",
        _ => "Review"
    };

    private sealed class ReviewRow
    {
        public Guid Id { get; init; }
        public ReviewType ReviewType { get; init; }
        public Guid SourceId { get; init; }
        public string SourceReference { get; init; } = string.Empty;
        public Guid TargetId { get; init; }
        public string TargetName { get; init; } = string.Empty;
        public Guid PatientProfileId { get; init; }
        public string PatientFullName { get; init; } = string.Empty;
        public int Rating { get; init; }
        public string? Comment { get; init; }
        public ReviewModerationStatus ModerationStatus { get; init; }
        public string? ModerationReason { get; init; }
        public DateTime? ModeratedAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}
