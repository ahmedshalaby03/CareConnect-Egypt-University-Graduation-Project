using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Notifications;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

public sealed class NotificationService : INotificationService
{
    private const int RecentLimit = 8;
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context) => _context = context;

    public async Task QueueAsync(CreateNotificationCommand command, CancellationToken ct = default)
    {
        ValidateTrustedCommand(command);

        var key = Normalise(command.DeduplicationKey);
        if (key is not null)
        {
            var trackedDuplicate = _context.ChangeTracker.Entries<Notification>()
                .Any(entry => entry.State != EntityState.Deleted &&
                              entry.Entity.DeduplicationKey == key);
            if (trackedDuplicate ||
                await _context.Notifications.AsNoTracking()
                    .AnyAsync(notification => notification.DeduplicationKey == key, ct))
            {
                return;
            }
        }

        _context.Notifications.Add(new Notification
        {
            RecipientApplicationUserId = command.RecipientApplicationUserId,
            Type = command.Type,
            Category = command.Category,
            Title = command.Title.Trim(),
            Message = command.Message.Trim(),
            RelatedEntityType = command.RelatedEntityType,
            RelatedEntityId = command.RelatedEntityId,
            ActionRoute = Normalise(command.ActionRoute),
            DeduplicationKey = key,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task QueueManyAsync(
        IEnumerable<CreateNotificationCommand> commands,
        CancellationToken ct = default)
    {
        foreach (var command in commands)
            await QueueAsync(command, ct);
    }

    public async Task<Result<PagedResult<NotificationDto>>> GetAsync(
        string userId, NotificationFilter filter, CancellationToken ct = default)
    {
        if (!await IsActiveUserAsync(userId, ct))
            return Forbidden<PagedResult<NotificationDto>>();

        var query = OwnedVisible(userId);
        if (filter.IsRead.HasValue) query = query.Where(n => n.IsRead == filter.IsRead.Value);
        if (filter.Category.HasValue) query = query.Where(n => n.Category == filter.Category.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(n => n.Title.Contains(term) || n.Message.Contains(term));
        }
        if (filter.DateFrom.HasValue)
            query = query.Where(n => n.CreatedAt >= filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (filter.DateTo.HasValue)
            query = query.Where(n => n.CreatedAt < filter.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));

        var total = await query.CountAsync(ct);
        query = filter.SortDirection == "asc"
            ? query.OrderBy(n => n.CreatedAt)
            : query.OrderByDescending(n => n.CreatedAt);
        var items = await query.Skip(filter.Skip).Take(filter.PageSize)
            .Select(Projection()).ToListAsync(ct);

        return Result<PagedResult<NotificationDto>>.Success(
            PagedResult<NotificationDto>.Create(items, filter.Page, filter.PageSize, total),
            "Notifications retrieved successfully.");
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> GetRecentAsync(
        string userId, CancellationToken ct = default)
    {
        if (!await IsActiveUserAsync(userId, ct))
            return Forbidden<IReadOnlyList<NotificationDto>>();

        var items = await OwnedVisible(userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(RecentLimit)
            .Select(Projection())
            .ToListAsync(ct);
        return Result<IReadOnlyList<NotificationDto>>.Success(
            items, "Recent notifications retrieved successfully.");
    }

    public async Task<Result<NotificationUnreadCountDto>> GetUnreadCountAsync(
        string userId, CancellationToken ct = default)
    {
        if (!await IsActiveUserAsync(userId, ct))
            return Forbidden<NotificationUnreadCountDto>();

        var count = await OwnedVisible(userId).CountAsync(n => !n.IsRead, ct);
        return Result<NotificationUnreadCountDto>.Success(new() { UnreadCount = count });
    }

    public Task<Result<NotificationDto>> MarkAsReadAsync(
        string userId, Guid notificationId, CancellationToken ct = default) =>
        SetReadStateAsync(userId, notificationId, true, ct);

    public Task<Result<NotificationDto>> MarkAsUnreadAsync(
        string userId, Guid notificationId, CancellationToken ct = default) =>
        SetReadStateAsync(userId, notificationId, false, ct);

    public async Task<Result<NotificationUnreadCountDto>> MarkAllAsReadAsync(
        string userId, CancellationToken ct = default)
    {
        if (!await IsActiveUserAsync(userId, ct))
            return Forbidden<NotificationUnreadCountDto>();

        var now = DateTime.UtcNow;
        await _context.Notifications
            .Where(n => n.RecipientApplicationUserId == userId &&
                        n.DismissedAt == null && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now)
                .SetProperty(n => n.UpdatedAt, now), ct);
        return Result<NotificationUnreadCountDto>.Success(
            new() { UnreadCount = 0 }, "All notifications marked as read.");
    }

    public async Task<Result<bool>> DismissAsync(
        string userId, Guid notificationId, CancellationToken ct = default)
    {
        if (!await IsActiveUserAsync(userId, ct))
            return Forbidden<bool>();

        var now = DateTime.UtcNow;
        var updated = await _context.Notifications
            .Where(n => n.Id == notificationId &&
                        n.RecipientApplicationUserId == userId &&
                        n.DismissedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.DismissedAt, now)
                .SetProperty(n => n.UpdatedAt, now), ct);
        return updated == 0
            ? Result<bool>.NotFound("Notification not found.")
            : Result<bool>.Success(true, "Notification dismissed.");
    }

    private async Task<Result<NotificationDto>> SetReadStateAsync(
        string userId, Guid notificationId, bool isRead, CancellationToken ct)
    {
        if (!await IsActiveUserAsync(userId, ct))
            return Forbidden<NotificationDto>();

        var now = DateTime.UtcNow;
        var updated = await _context.Notifications
            .Where(n => n.Id == notificationId &&
                        n.RecipientApplicationUserId == userId &&
                        n.DismissedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, isRead)
                .SetProperty(n => n.ReadAt, isRead ? now : null)
                .SetProperty(n => n.UpdatedAt, now), ct);
        if (updated == 0) return Result<NotificationDto>.NotFound("Notification not found.");

        var dto = await OwnedVisible(userId).Where(n => n.Id == notificationId)
            .Select(Projection()).FirstAsync(ct);
        return Result<NotificationDto>.Success(
            dto, isRead ? "Notification marked as read." : "Notification marked as unread.");
    }

    private IQueryable<Notification> OwnedVisible(string userId) =>
        _context.Notifications.AsNoTracking()
            .Where(n => n.RecipientApplicationUserId == userId && n.DismissedAt == null);

    private Task<bool> IsActiveUserAsync(string userId, CancellationToken ct) =>
        _context.Users.AsNoTracking().AnyAsync(user => user.Id == userId && user.IsActive, ct);

    private static System.Linq.Expressions.Expression<Func<Notification, NotificationDto>> Projection() =>
        notification => new NotificationDto
        {
            Id = notification.Id,
            Type = notification.Type,
            TypeName = notification.Type.ToString(),
            Category = notification.Category,
            CategoryName = notification.Category.ToString(),
            Title = notification.Title,
            Message = notification.Message,
            RelatedEntityType = notification.RelatedEntityType,
            RelatedEntityTypeName = notification.RelatedEntityType == null
                ? null
                : notification.RelatedEntityType.ToString(),
            RelatedEntityId = notification.RelatedEntityId,
            ActionRoute = notification.ActionRoute,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt
        };

    private static void ValidateTrustedCommand(CreateNotificationCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.RecipientApplicationUserId))
            throw new InvalidOperationException("A notification recipient is required.");
        if (string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length > 200)
            throw new InvalidOperationException("Notification title is invalid.");
        if (string.IsNullOrWhiteSpace(command.Message) || command.Message.Trim().Length > 1000)
            throw new InvalidOperationException("Notification message is invalid.");
        if (command.ActionRoute?.Trim().Length > 500)
            throw new InvalidOperationException("Notification action route is too long.");
        if (command.DeduplicationKey?.Trim().Length > 300)
            throw new InvalidOperationException("Notification deduplication key is too long.");
        if (!IsSafeRoute(command.ActionRoute))
            throw new InvalidOperationException("Notification action route must be an internal relative route.");
    }

    private static bool IsSafeRoute(string? route) =>
        string.IsNullOrWhiteSpace(route) ||
        (route.StartsWith('/') &&
         !route.Contains("//", StringComparison.Ordinal) &&
         !route.Contains("://", StringComparison.OrdinalIgnoreCase) &&
         !route.Contains("javascript:", StringComparison.OrdinalIgnoreCase));

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Result<T> Forbidden<T>() =>
        Result<T>.Failure(ResultStatus.Forbidden, "Your account is inactive.");
}
