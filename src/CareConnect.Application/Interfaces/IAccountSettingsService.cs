using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Accounts;

namespace CareConnect.Application.Interfaces;

public interface IAccountSettingsService
{
    Task<Result<AccountProfileDto>> GetCurrentAccountAsync(
        string userId,
        CancellationToken ct = default);

    Task<Result<AccountProfileDto>> UpdateCurrentAccountAsync(
        string userId,
        UpdateAccountProfileRequest request,
        CancellationToken ct = default);

    Task<Result<AccountProfileDto>> UploadProfileImageAsync(
        string userId,
        ProfileImageUpload upload,
        CancellationToken ct = default);

    Task<Result<AccountProfileDto>> DeleteProfileImageAsync(
        string userId,
        CancellationToken ct = default);
}
