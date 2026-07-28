using System.Threading.RateLimiting;
using CareConnect.Application.Common.Models;
using CareConnect.Domain.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace CareConnect.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string AiMedicalAssistantPolicy = "AiMedicalAssistant";

    public static IServiceCollection AddCareConnectRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse.Fail(
                        "You have reached the medical assistant request limit. Please wait a minute and try again."),
                    cancellationToken);
            };

            options.AddPolicy(AiMedicalAssistantPolicy, httpContext =>
            {
                var userId = httpContext.User.FindFirst(AppClaimTypes.UserId)?.Value
                    ?? "unauthenticated";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }
}
