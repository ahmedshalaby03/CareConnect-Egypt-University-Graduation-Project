using CareConnect.Application.Common.Models;
using CareConnect.Domain.Enums;

namespace CareConnect.Application.DTOs.Notifications;

public sealed class NotificationDto
{
    public Guid Id { get; init; }
    public NotificationType Type { get; init; }
    public string TypeName { get; init; } = string.Empty;
    public NotificationCategory Category { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public NotificationRelatedEntityType? RelatedEntityType { get; init; }
    public string? RelatedEntityTypeName { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? ActionRoute { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class NotificationUnreadCountDto
{
    public int UnreadCount { get; init; }
}

public sealed class NotificationFilter : PagedQueryParameters
{
    public bool? IsRead { get; set; }
    public NotificationCategory? Category { get; set; }
    public string? Search { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Trusted internal workflow input. It is deliberately never accepted by an API action.
/// </summary>
public sealed class CreateNotificationCommand
{
    public string RecipientApplicationUserId { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public NotificationCategory Category { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public NotificationRelatedEntityType? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? ActionRoute { get; init; }
    public string? DeduplicationKey { get; init; }
}
