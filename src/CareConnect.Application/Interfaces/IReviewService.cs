using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Reviews;
using CareConnect.Domain.Enums;

namespace CareConnect.Application.Interfaces;

public interface IReviewService
{
    Task<Result<ReviewEligibilityDto>> GetEligibilityAsync(string userId, ReviewType type, Guid sourceId, CancellationToken ct = default);
    Task<Result<ReviewDto>> GetPatientReviewAsync(string userId, ReviewType type, Guid sourceId, CancellationToken ct = default);
    Task<Result<ReviewDto>> CreateAsync(string userId, ReviewType type, Guid sourceId, SaveReviewRequest request, CancellationToken ct = default);
    Task<Result<ReviewDto>> UpdateAsync(string userId, ReviewType type, Guid sourceId, SaveReviewRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<ReviewDto>>> GetPatientReviewsAsync(string userId, PatientReviewFilter filter, CancellationToken ct = default);
}

public interface IRatingQueryService
{
    Task<Result<PagedResult<ReviewDto>>> GetPublicReviewsAsync(ReviewType type, Guid targetId, ReviewListFilter filter, CancellationToken ct = default);
    Task<Result<RatingSummaryDto>> GetPublicSummaryAsync(ReviewType type, Guid targetId, CancellationToken ct = default);
    Task<Result<PagedResult<ReviewDto>>> GetOwnerReviewsAsync(string userId, ReviewType type, ReviewListFilter filter, CancellationToken ct = default);
    Task<Result<RatingSummaryDto>> GetOwnerSummaryAsync(string userId, ReviewType type, CancellationToken ct = default);
}

public interface IReviewModerationService
{
    Task<Result<PagedResult<ReviewDto>>> GetAllAsync(string adminUserId, SuperAdminReviewFilter filter, CancellationToken ct = default);
    Task<Result<ReviewDto>> GetByIdAsync(string adminUserId, ReviewType type, Guid id, CancellationToken ct = default);
    Task<Result<ReviewDto>> HideAsync(string adminUserId, ReviewType type, Guid id, ModerateReviewRequest request, CancellationToken ct = default);
    Task<Result<ReviewDto>> RestoreAsync(string adminUserId, ReviewType type, Guid id, CancellationToken ct = default);
}
