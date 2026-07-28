using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Notifications;

namespace CareConnect.Application.Interfaces;

public interface INotificationService
{
    Task QueueAsync(CreateNotificationCommand command, CancellationToken ct = default);
    Task QueueManyAsync(IEnumerable<CreateNotificationCommand> commands, CancellationToken ct = default);

    Task<Result<PagedResult<NotificationDto>>> GetAsync(
        string userId, NotificationFilter filter, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NotificationDto>>> GetRecentAsync(
        string userId, CancellationToken ct = default);
    Task<Result<NotificationUnreadCountDto>> GetUnreadCountAsync(
        string userId, CancellationToken ct = default);
    Task<Result<NotificationDto>> MarkAsReadAsync(
        string userId, Guid notificationId, CancellationToken ct = default);
    Task<Result<NotificationDto>> MarkAsUnreadAsync(
        string userId, Guid notificationId, CancellationToken ct = default);
    Task<Result<NotificationUnreadCountDto>> MarkAllAsReadAsync(
        string userId, CancellationToken ct = default);
    Task<Result<bool>> DismissAsync(
        string userId, Guid notificationId, CancellationToken ct = default);
}
