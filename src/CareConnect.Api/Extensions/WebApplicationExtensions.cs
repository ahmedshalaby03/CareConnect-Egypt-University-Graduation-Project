using CareConnect.Infrastructure.Persistence;
using CareConnect.Infrastructure.Persistence.Seed;
using CareConnect.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

    /// <summary>
    /// Local-Development-only demo data. Runs only when the environment is Development and
    /// DemoData:Enabled is true; a disabled or missing switch leaves the database untouched.
    /// Applies any pending migration first (Development only) so the seeder never runs
    /// against a stale schema, then hands off to <see cref="IDemoDataSeeder"/>.
    /// </summary>
    public static async Task SeedDemoDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var environment = services.GetRequiredService<IWebHostEnvironment>();
        var options = services.GetRequiredService<IOptions<DemoDataOptions>>().Value;
        var logger = services.GetRequiredService<ILogger<Program>>();

        if (!environment.IsDevelopment())
        {
            return;
        }

        if (!options.Enabled)
        {
            logger.LogInformation("Demo data seeding is disabled (DemoData:Enabled is false). Skipping.");
            return;
        }

        var context = services.GetRequiredService<ApplicationDbContext>();

        try
        {
            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
            if (pendingMigrations.Count > 0)
            {
                logger.LogInformation(
                    "Applying {Count} pending Development migration(s): {Migrations}.",
                    pendingMigrations.Count, string.Join(", ", pendingMigrations));

                await context.Database.MigrateAsync();
            }
            else
            {
                logger.LogInformation("Existing migrations verified - no pending Development migrations.");
            }
        }
        catch (Exception ex)
        {
            // Preserve the database and stop here rather than seeding against a schema that
            // might not match the current model.
            logger.LogError(ex, "Applying pending Development migrations failed. Demo data was not seeded.");
            return;
        }

        try
        {
            var seeder = services.GetRequiredService<IDemoDataSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo data seeding failed.");
        }
    }
}
