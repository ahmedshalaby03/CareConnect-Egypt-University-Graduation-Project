using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Accounts;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Constants;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

public sealed class AccountSettingsService : IAccountSettingsService
{
    private const string InactiveMessage = "Your account is inactive.";

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProfileImageStorageService _images;
    private readonly ILogger<AccountSettingsService> _logger;

    public AccountSettingsService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IProfileImageStorageService images,
        ILogger<AccountSettingsService> logger)
    {
        _context = context;
        _userManager = userManager;
        _images = images;
        _logger = logger;
    }

    public async Task<Result<AccountProfileDto>> GetCurrentAccountAsync(
        string userId,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<AccountProfileDto>.NotFound("Account not found.");
        }

        if (!user.IsActive)
        {
            return Forbidden();
        }

        return Result<AccountProfileDto>.Success(
            await ToDtoAsync(user),
            "Account profile retrieved successfully.");
    }

    public async Task<Result<AccountProfileDto>> UpdateCurrentAccountAsync(
        string userId,
        UpdateAccountProfileRequest request,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<AccountProfileDto>.NotFound("Account not found.");
        }

        if (!user.IsActive)
        {
            return Forbidden();
        }

        var fullName = request.FullName.Trim();
        var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        if (phoneNumber is not null
            && await _context.Users.AnyAsync(
                candidate => candidate.Id != user.Id && candidate.PhoneNumber == phoneNumber,
                ct))
        {
            return Result<AccountProfileDto>.Conflict(
                "Another account already uses this phone number.");
        }

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;

        IdentityResult update;
        try
        {
            update = await _userManager.UpdateAsync(user);
        }
        catch (DbUpdateException)
        {
            return Result<AccountProfileDto>.Conflict(
                "The account could not be updated because the phone number is already in use.");
        }
        if (!update.Succeeded)
        {
            return Result<AccountProfileDto>.Invalid(
                "Account profile could not be updated.",
                update.Errors.Select(error => error.Description).ToList());
        }

        _logger.LogInformation("User {UserId} updated shared account information.", user.Id);
        return Result<AccountProfileDto>.Success(
            await ToDtoAsync(user),
            "Account information updated successfully.");
    }

    public async Task<Result<AccountProfileDto>> UploadProfileImageAsync(
        string userId,
        ProfileImageUpload upload,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<AccountProfileDto>.NotFound("Account not found.");
        }

        if (!user.IsActive)
        {
            return Forbidden();
        }

        var stored = await _images.SaveAsync(upload, ct);
        if (!stored.Succeeded || stored.Data is null)
        {
            return Result<AccountProfileDto>.Failure(
                stored.Status,
                stored.Message,
                stored.Errors);
        }

        var oldFileName = user.ProfileImageFileName;
        user.ProfileImageFileName = stored.Data.FileName;

        IdentityResult update;
        try
        {
            update = await _userManager.UpdateAsync(user);
        }
        catch
        {
            await _images.DeleteAsync(stored.Data.FileName, CancellationToken.None);
            throw;
        }
        if (!update.Succeeded)
        {
            await _images.DeleteAsync(stored.Data.FileName, ct);
            return Result<AccountProfileDto>.Invalid(
                "The profile image could not be attached to your account.",
                update.Errors.Select(error => error.Description).ToList());
        }

        await _images.DeleteAsync(oldFileName, ct);
        _logger.LogInformation(
            "User {UserId} replaced their managed profile image ({SizeBytes} bytes).",
            user.Id,
            stored.Data.SizeBytes);

        return Result<AccountProfileDto>.Success(
            await ToDtoAsync(user),
            "Profile image updated successfully.");
    }

    public async Task<Result<AccountProfileDto>> DeleteProfileImageAsync(
        string userId,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<AccountProfileDto>.NotFound("Account not found.");
        }

        if (!user.IsActive)
        {
            return Forbidden();
        }

        var oldFileName = user.ProfileImageFileName;
        if (oldFileName is null)
        {
            return Result<AccountProfileDto>.Success(
                await ToDtoAsync(user),
                "No profile image was set.");
        }

        user.ProfileImageFileName = null;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            user.ProfileImageFileName = oldFileName;
            return Result<AccountProfileDto>.Invalid(
                "The profile image could not be removed.",
                update.Errors.Select(error => error.Description).ToList());
        }

        await _images.DeleteAsync(oldFileName, ct);
        _logger.LogInformation("User {UserId} removed their managed profile image.", user.Id);

        return Result<AccountProfileDto>.Success(
            await ToDtoAsync(user),
            "Profile image removed successfully.");
    }

    private async Task<AccountProfileDto> ToDtoAsync(ApplicationUser user)
    {
        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;
        var profileImageUrl = _images.GetPublicUrl(user.ProfileImageFileName);

        return new AccountProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            ProfileImageUrl = profileImageUrl,
            HasProfileImage = profileImageUrl is not null,
            RoleProfileRoute = role switch
            {
                AppRoles.Doctor => "/dashboard/doctor/profile",
                AppRoles.Hospital => "/dashboard/hospital/profile",
                AppRoles.MedicalServiceProvider => "/dashboard/service-provider/profile",
                _ => null
            }
        };
    }

    private static Result<AccountProfileDto> Forbidden() =>
        Result<AccountProfileDto>.Failure(ResultStatus.Forbidden, InactiveMessage);
}
