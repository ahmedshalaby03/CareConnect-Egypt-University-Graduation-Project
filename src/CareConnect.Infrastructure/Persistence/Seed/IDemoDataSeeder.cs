namespace CareConnect.Infrastructure.Persistence.Seed;

/// <summary>
/// Populates the local Development database with realistic, idempotent demo data so the
/// Angular application has something to display. Never runs outside Development, and never
/// runs unless explicitly enabled - see <see cref="Settings.DemoDataOptions"/>.
/// </summary>
public interface IDemoDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
