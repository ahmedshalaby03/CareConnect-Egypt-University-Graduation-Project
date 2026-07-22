using CareConnect.Infrastructure.Persistence.Seed;

namespace CareConnect.Api.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Seeds roles and the SuperAdmin account. Migrations are deliberately NOT applied here -
    /// run 'dotnet ef database update' yourself so schema changes are never a surprise.
    /// </summary>
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            // A missing schema is the usual cause, and it should not stop the API from
            // starting: the developer just has not run the migration yet.
            logger.LogError(
                ex,
                "Database seeding failed. If the schema does not exist yet, run: " +
                "dotnet ef database update -p src/CareConnect.Infrastructure -s src/CareConnect.Api");
        }
    }
}
