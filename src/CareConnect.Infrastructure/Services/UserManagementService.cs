using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Admin;
using CareConnect.Application.DTOs.Auth;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Constants;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(ApplicationDbContext context, ILogger<UserManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PagedResult<UserDto>>> GetUsersAsync(
        UserQueryParameters query,
        CancellationToken ct = default)
    {
        if (query.Role is not null && !AppRoles.All.Contains(query.Role, StringComparer.Ordinal))
        {
            return Result<PagedResult<UserDto>>.Invalid(
                "Unknown role filter.",
                [$"Role must be one of: {string.Join(", ", AppRoles.All)}."]);
        }

        // Left joins, so a user with no role assignment is still listed rather than
        // silently vanishing from the admin screen.
        var baseQuery =
            from user in _context.Users.AsNoTracking()
            join userRole in _context.UserRoles on user.Id equals userRole.UserId into userRoles
            from userRole in userRoles.DefaultIfEmpty()
            join role in _context.Roles on userRole.RoleId equals role.Id into roles
            from role in roles.DefaultIfEmpty()
            select new { User = user, RoleName = role != null ? role.Name : null };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            baseQuery = baseQuery.Where(x =>
                EF.Functions.Like(x.User.FullName, $"%{term}%") ||
                EF.Functions.Like(x.User.Email!, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            baseQuery = baseQuery.Where(x => x.RoleName == query.Role);
        }

        if (query.IsActive.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.User.IsActive == query.IsActive.Value);
        }

        var totalCount = await baseQuery.CountAsync(ct);

        // Projected explicitly: PasswordHash, SecurityStamp and refresh tokens never leave
        // the database on this path.
        var items = await baseQuery
            .OrderByDescending(x => x.User.CreatedAt)
            .ThenBy(x => x.User.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new UserDto
            {
                Id = x.User.Id,
                FullName = x.User.FullName,
                Email = x.User.Email ?? string.Empty,
                PhoneNumber = x.User.PhoneNumber,
                Role = x.RoleName ?? string.Empty,
                IsActive = x.User.IsActive,
                CreatedAt = x.User.CreatedAt,
                LastLoginAt = x.User.LastLoginAt
            })
            .ToListAsync(ct);

        return Result<PagedResult<UserDto>>.Success(
            PagedResult<UserDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Users retrieved successfully.");
    }

    public async Task<Result<ToggleUserStatusResponse>> ToggleUserStatusAsync(
        string userId,
        string actingUserId,
        CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Result<ToggleUserStatusResponse>.NotFound("User not found.");
        }

        if (user.Id == actingUserId)
        {
            return Result<ToggleUserStatusResponse>.Failure(
                ResultStatus.Forbidden,
                "You cannot change the status of your own account.");
        }

        var isSuperAdmin = await (
            from userRole in _context.UserRoles
            join role in _context.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == user.Id && role.Name == AppRoles.SuperAdmin
            select role.Id).AnyAsync(ct);

        if (isSuperAdmin)
        {
            return Result<ToggleUserStatusResponse>.Failure(
                ResultStatus.Forbidden,
                "Administrator accounts cannot be deactivated.");
        }

        user.IsActive = !user.IsActive;

        if (!user.IsActive)
        {
            // Deactivation has to take effect immediately, not when the access token expires.
            var now = DateTime.UtcNow;
            var activeTokens = await _context.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null && t.ExpiresAt > now)
                .ToListAsync(ct);

            foreach (var token in activeTokens)
            {
                token.RevokedAt = now;
                token.RevokedReason = "Account deactivated by an administrator.";
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Administrator {ActingUserId} set IsActive={IsActive} on user {UserId}.",
            actingUserId, user.IsActive, user.Id);

        return Result<ToggleUserStatusResponse>.Success(
            new ToggleUserStatusResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                IsActive = user.IsActive
            },
            user.IsActive ? "User activated successfully." : "User deactivated successfully.");
    }
}
