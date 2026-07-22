namespace CareConnect.Domain.Constants;

/// <summary>
/// The single source of truth for role names. Never accept a raw role string from a client
/// without checking it against <see cref="PublicRoles"/> first.
/// </summary>
public static class AppRoles
{
    public const string Patient = "Patient";
    public const string Doctor = "Doctor";
    public const string Hospital = "Hospital";
    public const string MedicalServiceProvider = "MedicalServiceProvider";
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>Roles a visitor is allowed to pick on the registration form.</summary>
    public static readonly IReadOnlyList<string> PublicRoles =
    [
        Patient,
        Doctor,
        Hospital,
        MedicalServiceProvider
    ];

    /// <summary>Every role that gets seeded into the database.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        Patient,
        Doctor,
        Hospital,
        MedicalServiceProvider,
        SuperAdmin
    ];

    public static bool IsPublicRole(string? role) =>
        role is not null && PublicRoles.Contains(role, StringComparer.Ordinal);
}
