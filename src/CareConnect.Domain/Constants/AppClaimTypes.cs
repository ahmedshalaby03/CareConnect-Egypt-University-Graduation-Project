namespace CareConnect.Domain.Constants;

/// <summary>
/// Short, explicit JWT claim names. We keep these instead of the long
/// schemas.xmlsoap.org URIs so the token stays small and readable on the client.
/// </summary>
public static class AppClaimTypes
{
    public const string UserId = "sub";
    public const string FullName = "fullName";
    public const string Email = "email";
    public const string Role = "role";
}
