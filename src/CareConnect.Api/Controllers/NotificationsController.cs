using CareConnect.Api.Common;
using CareConnect.Application.DTOs.Notifications;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notifications;
    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] NotificationFilter filter, CancellationToken ct) =>
        FromResult(await _notifications.GetAsync(CurrentUserId, filter, ct));

    [HttpGet("recent")]
    public async Task<IActionResult> Recent(CancellationToken ct) =>
        FromResult(await _notifications.GetRecentAsync(CurrentUserId, ct));

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct) =>
        FromResult(await _notifications.GetUnreadCountAsync(CurrentUserId, ct));

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct) =>
        FromResult(await _notifications.MarkAsReadAsync(CurrentUserId, id, ct));

    [HttpPatch("{id:guid}/unread")]
    public async Task<IActionResult> MarkUnread(Guid id, CancellationToken ct) =>
        FromResult(await _notifications.MarkAsUnreadAsync(CurrentUserId, id, ct));

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct) =>
        FromResult(await _notifications.MarkAllAsReadAsync(CurrentUserId, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Dismiss(Guid id, CancellationToken ct) =>
        FromResult(await _notifications.DismissAsync(CurrentUserId, id, ct));
}
