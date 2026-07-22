using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Auth;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    /// <summary>Creates a Patient, Doctor, Hospital or MedicalServiceProvider account.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Exchanges credentials for an access token and a refresh token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, _currentUser.IpAddress, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Exchanges a valid refresh token for a fresh pair. The presented token is rotated out
    /// and cannot be used again.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request, _currentUser.IpAddress, ct);
        return FromResult(result);
    }

    /// <summary>Revokes one of the caller's own refresh tokens.</summary>
    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeToken(RevokeTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RevokeTokenAsync(request, CurrentUserId, _currentUser.IpAddress, ct);
        return FromResult(result);
    }

    /// <summary>Changes the caller's password and signs every session out.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var result = await _authService.ChangePasswordAsync(CurrentUserId, request, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Signs out. With a refresh token in the body only that session ends; without one,
    /// every session for the account ends.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(LogoutRequest? request, CancellationToken ct)
    {
        var result = await _authService.LogoutAsync(
            CurrentUserId, request ?? new LogoutRequest(), _currentUser.IpAddress, ct);

        return FromResult(result);
    }

    /// <summary>Returns the profile of the currently authenticated user.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await _authService.GetCurrentUserAsync(CurrentUserId, ct);
        return FromResult(result);
    }
}
