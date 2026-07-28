using System.Text.Json;
using CareConnect.Api.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CareConnect.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCareConnectHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseReadinessHealthCheck>("sql-server", tags: ["ready"]);

        return services;
    }

    public static WebApplication MapCareConnectHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteControlledResponseAsync
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = WriteControlledResponseAsync
        }).AllowAnonymous();

        return app;
    }

    private static Task WriteControlledResponseAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy"
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
