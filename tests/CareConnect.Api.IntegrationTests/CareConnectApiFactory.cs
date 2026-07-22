using System.Data.Common;
using CareConnect.Infrastructure.Persistence;
using CareConnect.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CareConnect.Api.IntegrationTests;

/// <summary>
/// Hosts the real API in memory on top of a private SQLite database.
///
/// SQLite rather than SQL Server on purpose: these tests must never touch a developer's
/// database, and the schema is created with EnsureCreated, so running them neither applies
/// nor requires a migration.
/// </summary>
public class CareConnectApiFactory : WebApplicationFactory<Program>
{
    public const string SuperAdminEmail = "admin@careconnect.test";
    public const string SuperAdminPassword = "SeededAdminPass123!";

    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting, not ConfigureAppConfiguration: Program.cs reads these values while it
        // runs, which happens before any ConfigureAppConfiguration callback is applied.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "DataSource=:memory:");
        builder.UseSetting("Jwt:Key", "integration-test-signing-key-that-is-comfortably-longer-than-32-chars");
        builder.UseSetting("SuperAdmin:Email", SuperAdminEmail);
        builder.UseSetting("SuperAdmin:Password", SuperAdminPassword);
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:4200");

        builder.ConfigureServices(services =>
        {
            // One shared connection keeps the in-memory database alive for the whole
            // fixture: SQLite discards it as soon as the last connection closes.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // EF 9+ keeps the provider selection in IDbContextOptionsConfiguration, so
            // dropping DbContextOptions alone would leave SQL Server registered as well and
            // EF refuses to run with two providers.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Program.cs already ran its own seeding pass and logged a failure, because at that
        // point the schema did not exist yet. Create it now and seed for real.
        using var scope = host.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        scope.ServiceProvider.GetRequiredService<DatabaseSeeder>()
            .SeedAsync()
            .GetAwaiter()
            .GetResult();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}
