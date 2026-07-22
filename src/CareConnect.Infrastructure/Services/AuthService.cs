using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Auth;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Constants;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";
    private const string InactiveAccountMessage =
        "This account has been deactivated. Please contact the CareConnect administrator.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IJwtService jwtService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    // ---------------------------------------------------------------- Register

    public async Task<Result<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct = default)
    {
        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();
        var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        // Re-checked here even though FluentValidation already ran: this service must be
        // safe to call from anywhere, and role escalation is the one thing we cannot get wrong.
        if (!AppRoles.IsPublicRole(request.Role))
        {
            _logger.LogWarning("Rejected registration with disallowed role {Role}.", request.Role);
            return Result<RegisterResponse>.Invalid(
                "The selected role is not available for registration.",
                [$"Role must be one of: {string.Join(", ", AppRoles.PublicRoles)}."]);
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return Result<RegisterResponse>.Conflict("An account with this email already exists.");
        }

        if (phoneNumber is not null &&
            await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber, ct))
        {
            return Result<RegisterResponse>.Conflict("An account with this phone number already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            PhoneNumber = phoneNumber,
            FullName = fullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            // Academic scope: there is no mail server yet, so accounts start confirmed.
            EmailConfirmed = true
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(ct);
            return Result<RegisterResponse>.Invalid(
                "Registration failed.",
                createResult.Errors.Select(e => e.Description).ToList());
        }

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync(ct);
            return Result<RegisterResponse>.Invalid(
                "Registration failed while assigning the selected role.",
                roleResult.Errors.Select(e => e.Description).ToList());
        }

        AddProfileForRole(user.Id, request.Role);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Registered user {UserId} with role {Role}.", user.Id, request.Role);

        return Result<RegisterResponse>.Success(
            new RegisterResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Role = request.Role
            },
            "User registered successfully.");
    }

    private void AddProfileForRole(string userId, string role)
    {
        switch (role)
        {
            case AppRoles.Patient:
                _context.PatientProfiles.Add(new PatientProfile { UserId = userId });
                break;
            case AppRoles.Doctor:
                _context.DoctorProfiles.Add(new DoctorProfile { UserId = userId });
                break;
            case AppRoles.Hospital:
                _context.HospitalProfiles.Add(new HospitalProfile { UserId = userId });
                break;
            case AppRoles.MedicalServiceProvider:
                _context.MedicalServiceProviderProfiles.Add(
                    new MedicalServiceProviderProfile { UserId = userId });
                break;
            default:
                throw new InvalidOperationException($"No profile is defined for role '{role}'.");
        }
    }

    // ------------------------------------------------------------------- Login

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var email = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);

        // Credentials are checked before the active flag so a wrong password never reveals
        // whether the address belongs to a real account.
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            if (user is not null)
            {
                await _userManager.AccessFailedAsync(user);
            }

            return Result<AuthResponse>.Unauthorized(InvalidCredentialsMessage);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Result<AuthResponse>.Failure(
                ResultStatus.Forbidden,
                "This account is temporarily locked after too many failed sign-in attempts. Try again later.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Blocked sign-in for deactivated user {UserId}.", user.Id);
            return Result<AuthResponse>.Failure(ResultStatus.Forbidden, InactiveAccountMessage);
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var role = await GetPrimaryRoleAsync(user);
        var response = await IssueTokensAsync(user, role, ipAddress, ct);

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} signed in.", user.Id);
        return Result<AuthResponse>.Success(response, "Signed in successfully.");
    }

    // ----------------------------------------------------------- Refresh token

    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var hash = _jwtService.HashRefreshToken(request.RefreshToken);

        var stored = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
        {
            return Result<AuthResponse>.Unauthorized("Invalid refresh token.");
        }

        if (stored.IsRevoked)
        {
            // A token that was rotated out and is now being presented again is a leak: the
            // legitimate client already swapped it for a new one, so somebody else has a copy.
            // Kill the whole family, which signs out both the attacker and the victim.
            //
            // A token revoked by an explicit logout or revoke has no replacement, and a late
            // replay of it is ordinary. That just fails, without punishing the other sessions.
            if (stored.ReplacedByTokenHash is not null)
            {
                _logger.LogWarning(
                    "Reuse of a rotated refresh token detected for user {UserId}.", stored.UserId);

                await RevokeAllActiveTokensAsync(
                    stored.UserId, ipAddress, "Refresh token reuse detected.", ct);

                await _context.SaveChangesAsync(ct);
            }

            return Result<AuthResponse>.Unauthorized("Invalid refresh token.");
        }

        if (stored.IsExpired)
        {
            return Result<AuthResponse>.Unauthorized("The refresh token has expired. Please sign in again.");
        }

        var user = stored.User;
        if (user is null)
        {
            return Result<AuthResponse>.Unauthorized("Invalid refresh token.");
        }

        if (!user.IsActive)
        {
            await RevokeAllActiveTokensAsync(user.Id, ipAddress, "Account deactivated.", ct);
            await _context.SaveChangesAsync(ct);
            return Result<AuthResponse>.Failure(ResultStatus.Forbidden, InactiveAccountMessage);
        }

        var role = await GetPrimaryRoleAsync(user);
        var response = await IssueTokensAsync(user, role, ipAddress, ct);

        // Rotation: the presented token dies the moment its replacement is issued.
        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedByIp = ipAddress;
        stored.RevokedReason = "Replaced by a new token during refresh.";
        stored.ReplacedByTokenHash = _jwtService.HashRefreshToken(response.RefreshToken);

        await _context.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(response, "Token refreshed successfully.");
    }

    // ------------------------------------------------------------ Revoke token

    public async Task<Result<bool>> RevokeTokenAsync(
        RevokeTokenRequest request,
        string requestingUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var hash = _jwtService.HashRefreshToken(request.RefreshToken);

        var stored = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        // Same response whether the token does not exist or belongs to somebody else, so
        // this endpoint cannot be used to probe for valid tokens.
        if (stored is null || stored.UserId != requestingUserId)
        {
            return Result<bool>.NotFound("Refresh token not found.");
        }

        if (!stored.IsActive)
        {
            return Result<bool>.Invalid("This refresh token is already revoked or expired.");
        }

        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedByIp = ipAddress;
        stored.RevokedReason = "Revoked by the user.";

        await _context.SaveChangesAsync(ct);

        return Result<bool>.Success(true, "Refresh token revoked successfully.");
    }

    // --------------------------------------------------------- Change password

    public async Task<Result<bool>> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<bool>.NotFound("User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(
            user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            return Result<bool>.Invalid(
                "Password change failed.",
                result.Errors.Select(e => e.Description).ToList());
        }

        // Everything issued under the old password is now untrusted.
        await RevokeAllActiveTokensAsync(user.Id, null, "Password changed.", ct);
        await _userManager.UpdateSecurityStampAsync(user);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} changed their password.", user.Id);
        return Result<bool>.Success(true, "Password changed successfully. Please sign in again.");
    }

    // ------------------------------------------------------------------ Logout

    public async Task<Result<bool>> LogoutAsync(
        string userId,
        LogoutRequest request,
        string? ipAddress,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var hash = _jwtService.HashRefreshToken(request.RefreshToken);
            var stored = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId, ct);

            if (stored is { RevokedAt: null })
            {
                stored.RevokedAt = DateTime.UtcNow;
                stored.RevokedByIp = ipAddress;
                stored.RevokedReason = "Signed out.";
            }
        }
        else
        {
            // No token supplied: sign the account out everywhere.
            await RevokeAllActiveTokensAsync(userId, ipAddress, "Signed out.", ct);
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} signed out.", userId);
        return Result<bool>.Success(true, "Signed out successfully.");
    }

    // -------------------------------------------------------------------- /me

    public async Task<Result<UserDto>> GetCurrentUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<UserDto>.NotFound("User not found.");
        }

        if (!user.IsActive)
        {
            return Result<UserDto>.Failure(ResultStatus.Forbidden, InactiveAccountMessage);
        }

        var role = await GetPrimaryRoleAsync(user);
        return Result<UserDto>.Success(ToDto(user, role), "Current user retrieved successfully.");
    }

    // ------------------------------------------------------------------ Shared

    private async Task<string> GetPrimaryRoleAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? string.Empty;
    }

    private async Task<AuthResponse> IssueTokensAsync(
        ApplicationUser user,
        string role,
        string? ipAddress,
        CancellationToken ct)
    {
        var accessToken = _jwtService.CreateAccessToken(user, role);
        var refreshToken = _jwtService.CreateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshToken.TokenHash,
            ExpiresAt = refreshToken.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        });

        await RemoveStaleTokensAsync(user.Id, ct);

        return new AuthResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken.RawToken,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            User = ToDto(user, role)
        };
    }

    private async Task RevokeAllActiveTokensAsync(
        string userId,
        string? ipAddress,
        string reason,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var active = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var token in active)
        {
            token.RevokedAt = now;
            token.RevokedByIp = ipAddress;
            token.RevokedReason = reason;
        }
    }

    /// <summary>Housekeeping so the table does not grow without bound over a long project life.</summary>
    private async Task RemoveStaleTokensAsync(string userId, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        var stale = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.CreatedAt < cutoff)
            .ToListAsync(ct);

        if (stale.Count > 0)
        {
            _context.RefreshTokens.RemoveRange(stale);
        }
    }

    private static UserDto ToDto(ApplicationUser user, string role) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email ?? string.Empty,
        PhoneNumber = user.PhoneNumber,
        Role = role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };
}
