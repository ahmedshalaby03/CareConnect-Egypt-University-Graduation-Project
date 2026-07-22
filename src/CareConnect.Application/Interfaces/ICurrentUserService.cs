namespace CareConnect.Application.Interfaces;

/// <summary>Reads the caller's identity from the current request, so services never touch HttpContext.</summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
}
