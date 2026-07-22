using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Admin;
using CareConnect.Application.DTOs.Auth;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// Platform administration. Guarded at the controller level, so every action added here
/// later is protected by default rather than by remembering to add an attribute.
/// </summary>
[Route("api/super-admin")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
public class SuperAdminController : ApiControllerBase
{
    private readonly IUserManagementService _userManagement;

    public SuperAdminController(IUserManagementService userManagement) =>
        _userManagement = userManagement;

    /// <summary>Lists platform users with search, role and status filters, paginated.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers([FromQuery] UserQueryParameters query, CancellationToken ct)
    {
        var result = await _userManagement.GetUsersAsync(query, ct);
        return FromResult(result);
    }

    /// <summary>Activates or deactivates a user. A deactivated user cannot sign in.</summary>
    [HttpPatch("users/{userId}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse<ToggleUserStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleUserStatus(string userId, CancellationToken ct)
    {
        var result = await _userManagement.ToggleUserStatusAsync(userId, CurrentUserId, ct);
        return FromResult(result);
    }
}
