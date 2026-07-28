namespace CareConnect.Application.DTOs.Accounts;

public class AccountProfileDto
{
    public string Id { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ProfileImageUrl { get; init; }
    public bool HasProfileImage { get; init; }

    /// <summary>
    /// Existing role-specific business-profile route, or null when that role has no
    /// separate profile page. The account page never edits business-profile fields.
    /// </summary>
    public string? RoleProfileRoute { get; init; }
}

public class UpdateAccountProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Transport-neutral upload passed from the API controller to the Application contract.
/// The storage implementation validates every value and never uses ClientFileName as a
/// destination path.
/// </summary>
public sealed record ProfileImageUpload(
    Stream Content,
    long Length,
    string ContentType,
    string ClientFileName);

public sealed record StoredProfileImage(
    string FileName,
    string PublicUrl,
    long SizeBytes,
    string ContentType);
