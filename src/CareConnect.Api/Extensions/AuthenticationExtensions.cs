using System.Text;
using CareConnect.Application.Common.Models;
using CareConnect.Domain.Constants;
using CareConnect.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CareConnect.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");

        if (string.IsNullOrWhiteSpace(settings.Key) || settings.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key is missing or shorter than 32 characters. Set it with " +
                "'dotnet user-secrets set \"Jwt:Key\" \"<a long random value>\"' or in appsettings.Development.json.");
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Keep the short claim names exactly as issued instead of expanding them
                // into the long WS-Federation URIs.
                options.MapInboundClaims = false;
                options.SaveToken = true;
                options.RequireHttpsMetadata = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(settings.ClockSkewSeconds),
                    NameClaimType = AppClaimTypes.FullName,
                    RoleClaimType = AppClaimTypes.Role
                };

                // Without these, a rejected request returns an empty body and the Angular
                // client has nothing to show the user.
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        if (context.Response.HasStarted)
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var message = context.AuthenticateFailure is not null
                            ? "Your session is invalid or has expired. Please sign in again."
                            : "Authentication is required to access this resource.";

                        await context.Response.WriteAsJsonAsync(ApiResponse.Fail(message));
                    },
                    OnForbidden = async context =>
                    {
                        if (context.Response.HasStarted)
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        await context.Response.WriteAsJsonAsync(
                            ApiResponse.Fail("You do not have permission to access this resource."));
                    }
                };
            });

        return services;
    }
}
