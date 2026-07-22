using Microsoft.OpenApi;

namespace CareConnect.Api.Extensions;

public static class SwaggerExtensions
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CareConnect Egypt API",
                Version = "v1",
                Description =
                    "Authentication and user management for the CareConnect Egypt healthcare platform."
            });

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the access token only - Swagger adds the 'Bearer ' prefix for you."
            });

            // Applies the bearer scheme to every operation, so the Authorize button in the
            // Swagger UI actually sends the token.
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference(BearerScheme, document, null), new List<string>() }
            });

            // Two DTO classes with the same name in different folders would otherwise
            // collide in the generated document.
            options.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
        });

        return services;
    }
}
