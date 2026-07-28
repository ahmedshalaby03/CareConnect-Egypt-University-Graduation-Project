using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Accounts;

namespace CareConnect.Application.Interfaces;

/// <summary>
/// Replaceable managed-image store. The Application layer deals only in streams, generated
/// file names and public URLs; it never knows a physical server path.
/// </summary>
public interface IProfileImageStorageService
{
    Task<Result<StoredProfileImage>> SaveAsync(
        ProfileImageUpload upload,
        CancellationToken ct = default);

    Task DeleteAsync(string? managedFileName, CancellationToken ct = default);

    string? GetPublicUrl(string? managedFileName);
}
