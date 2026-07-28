using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

public interface IVerifiedReview
{
    ReviewModerationStatus ModerationStatus { get; set; }
    string? ModerationReason { get; set; }
    string? ModeratedByApplicationUserId { get; set; }
    DateTime? ModeratedAt { get; set; }
}
