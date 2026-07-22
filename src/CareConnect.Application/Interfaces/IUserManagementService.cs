using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Admin;
using CareConnect.Application.DTOs.Auth;

namespace CareConnect.Application.Interfaces;

public interface IUserManagementService
{
    Task<Result<PagedResult<UserDto>>> GetUsersAsync(UserQueryParameters query, CancellationToken ct = default);

    Task<Result<ToggleUserStatusResponse>> ToggleUserStatusAsync(string userId, string actingUserId, CancellationToken ct = default);
}
