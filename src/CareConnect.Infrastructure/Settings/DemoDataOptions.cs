namespace CareConnect.Infrastructure.Settings;

/// <summary>
/// Controls the local-Development demo data seeder. Defaults to disabled so a missing
/// configuration section never accidentally turns seeding on.
/// </summary>
public class DemoDataOptions
{
    public const string SectionName = "DemoData";

    public bool Enabled { get; set; }
}
