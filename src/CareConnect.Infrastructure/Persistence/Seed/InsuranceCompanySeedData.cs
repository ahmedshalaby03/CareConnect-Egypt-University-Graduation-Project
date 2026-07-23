namespace CareConnect.Infrastructure.Persistence.Seed;

public record InsuranceCompanySeed(string Name, string ArabicName, string Description);

/// <summary>
/// Academic demo data. Seeding matches on Name, so adding entries here later tops the
/// table up without disturbing anything the SuperAdmin has edited.
/// </summary>
public static class InsuranceCompanySeedData
{
    public static readonly IReadOnlyList<InsuranceCompanySeed> Items =
    [
        new("Misr Insurance", "مصر للتأمين", "Egypt's oldest and largest state-owned insurer."),
        new("AXA Egypt", "أكسا مصر", "Local arm of the global AXA insurance group."),
        new("Allianz Egypt", "أليانز مصر", "Local arm of the global Allianz insurance group."),
        new("MetLife Egypt", "ميت لايف مصر", "Life and health insurance provider operating in Egypt."),
        new("Med Right", "ميد رايت", "Third-party medical insurance administrator."),
        new("GlobeMed Egypt", "جلوب ميد مصر", "Regional third-party healthcare benefits administrator.")
    ];
}
