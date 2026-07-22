using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Auth;

namespace CareConnect.Application.Interfaces;

public interface IAuthService
{
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);

    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken ct = default);

    /// <param name="requestingUserId">The caller. A user may only revoke their own tokens.</param>
    Task<Result<bool>> RevokeTokenAsync(RevokeTokenRequest request, string requestingUserId, string? ipAddress, CancellationToken ct = default);

    Task<Result<bool>> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default);

    Task<Result<bool>> LogoutAsync(string userId, LogoutRequest request, string? ipAddress, CancellationToken ct = default);

    Task<Result<UserDto>> GetCurrentUserAsync(string userId, CancellationToken ct = default);
}
