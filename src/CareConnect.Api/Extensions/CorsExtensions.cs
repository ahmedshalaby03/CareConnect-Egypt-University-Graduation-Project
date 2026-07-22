namespace CareConnect.Api.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "CareConnectClient";

    /// <summary>
    /// Allows only the origins listed under Cors:AllowedOrigins. The Angular dev server
    /// (http://localhost:4200) is configured in appsettings.Development.json.
    /// </summary>
    public static IServiceCollection AddCareConnectCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (allowedOrigins.Length == 0)
                {
                    // No origins configured: deny cross-origin calls rather than open the API up.
                    policy.WithOrigins().AllowAnyHeader().AllowAnyMethod();
                    return;
                }

                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
