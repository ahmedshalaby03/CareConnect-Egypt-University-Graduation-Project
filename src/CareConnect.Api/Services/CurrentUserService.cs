using System.Security.Claims;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Constants;

namespace CareConnect.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(AppClaimTypes.UserId);

    public string? Email => User?.FindFirstValue(AppClaimTypes.Email);

    public string? FullName => User?.FindFirstValue(AppClaimTypes.FullName);

    public string? Role => User?.FindFirstValue(AppClaimTypes.Role);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Prefers X-Forwarded-For so the recorded address stays useful once the API sits
    /// behind a reverse proxy.
    /// </summary>
    public string? IpAddress
    {
        get
        {
            var context = _accessor.HttpContext;
            if (context is null)
            {
                return null;
            }

            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
            {
                var first = forwarded.ToString().Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrWhiteSpace(first))
                {
                    return first;
                }
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
