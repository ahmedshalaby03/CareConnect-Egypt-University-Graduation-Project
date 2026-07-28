using CareConnect.Api.Common;
using CareConnect.Application.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Accounts;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/account")]
[Authorize]
[Produces("application/json")]
public sealed class AccountController : ApiControllerBase
{
    private readonly IAccountSettingsService _accounts;

    public AccountController(IAccountSettingsService accounts)
    {
        _accounts = accounts;
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<AccountProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await _accounts.GetCurrentAccountAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<AccountProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProfile(
        UpdateAccountProfileRequest request,
        CancellationToken ct)
    {
        var result = await _accounts.UpdateCurrentAccountAsync(CurrentUserId, request, ct);
        return FromResult(result);
    }

    [HttpPost("profile-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ProfileImageStorageConstants.MaximumUploadBytes + 512 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<AccountProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> UploadProfileImage(
        IFormFile? image,
        CancellationToken ct)
    {
        if (image is null)
        {
            return FromResult(Result<AccountProfileDto>.Invalid(
                "Profile image upload failed.",
                ["Select an image to upload."]));
        }

        if (image.Length > ProfileImageStorageConstants.MaximumUploadBytes)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                ApiResponse<AccountProfileDto>.Fail(
                    "Profile image upload failed.",
                    ["The image must not exceed 5 MB."]));
        }

        await using var stream = image.OpenReadStream();
        var upload = new ProfileImageUpload(
            stream,
            image.Length,
            image.ContentType,
            image.FileName);

        var result = await _accounts.UploadProfileImageAsync(CurrentUserId, upload, ct);
        return FromResult(result);
    }

    [HttpDelete("profile-image")]
    [ProducesResponseType(typeof(ApiResponse<AccountProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteProfileImage(CancellationToken ct)
    {
        var result = await _accounts.DeleteProfileImageAsync(CurrentUserId, ct);
        return FromResult(result);
    }
}
