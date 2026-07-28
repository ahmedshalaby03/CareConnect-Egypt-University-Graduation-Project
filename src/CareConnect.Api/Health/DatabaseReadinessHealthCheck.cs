using CareConnect.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CareConnect.Api.Health;

public sealed class DatabaseReadinessHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _db;

    public DatabaseReadinessHealthCheck(ApplicationDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch
        {
            // Health responses intentionally contain no provider, server or credential details.
            return HealthCheckResult.Unhealthy();
        }
    }
}
