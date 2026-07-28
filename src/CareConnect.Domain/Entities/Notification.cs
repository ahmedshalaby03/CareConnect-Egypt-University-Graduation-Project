using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RecipientApplicationUserId { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationRelatedEntityType? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? ActionRoute { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DismissedAt { get; set; }
    public string? DeduplicationKey { get; set; }

    public ApplicationUser? RecipientApplicationUser { get; set; }
}
