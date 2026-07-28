using CareConnect.Application.Common.Models;
using CareConnect.Domain.Enums;

namespace CareConnect.Application.DTOs.Reviews;

public sealed class SaveReviewRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public sealed class ModerateReviewRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class ReviewEligibilityDto
{
    public bool IsEligible { get; init; }
    public bool HasReview { get; init; }
    public Guid? ReviewId { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class ReviewDto
{
    public Guid Id { get; init; }
    public ReviewType ReviewType { get; init; }
    public string ReviewTypeName { get; init; } = string.Empty;
    public Guid SourceId { get; init; }
    public string SourceReference { get; init; } = string.Empty;
    public Guid TargetId { get; init; }
    public string TargetName { get; init; } = string.Empty;
    public string PatientDisplayName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public ReviewModerationStatus ModerationStatus { get; init; }
    public string ModerationStatusName { get; init; } = string.Empty;
    public string? ModerationReason { get; init; }
    public DateTime? ModeratedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public bool IsVerifiedInteraction { get; init; } = true;
}

public sealed class RatingDistributionDto
{
    public int OneStar { get; init; }
    public int TwoStars { get; init; }
    public int ThreeStars { get; init; }
    public int FourStars { get; init; }
    public int FiveStars { get; init; }
}

public sealed class RatingSummaryDto
{
    public double? AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public RatingDistributionDto Distribution { get; init; } = new();
}

public sealed class PatientReviewFilter : PagedQueryParameters
{
    public ReviewType? ReviewType { get; set; }
    public string? Search { get; set; }
    public int? Rating { get; set; }
    public ReviewModerationStatus? ModerationStatus { get; set; }
    public string SortBy { get; set; } = "newest";
}

public sealed class ReviewListFilter : PagedQueryParameters
{
    public int? Rating { get; set; }
    public string? Search { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string SortBy { get; set; } = "newest";
}

public sealed class SuperAdminReviewFilter : PagedQueryParameters
{
    public ReviewType? ReviewType { get; set; }
    public ReviewModerationStatus? ModerationStatus { get; set; }
    public int? Rating { get; set; }
    public string? Search { get; set; }
    public string? PatientName { get; set; }
    public string? TargetName { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string SortBy { get; set; } = "newest";
}
