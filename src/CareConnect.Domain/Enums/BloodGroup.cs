namespace CareConnect.Domain.Enums;

/// <summary>
/// The eight standard ABO/Rh blood groups. Explicit numeric values so a stored value never
/// silently shifts meaning if a case is inserted later.
/// </summary>
public enum BloodGroup
{
    APositive = 1,
    ANegative = 2,
    BPositive = 3,
    BNegative = 4,
    ABPositive = 5,
    ABNegative = 6,
    OPositive = 7,
    ONegative = 8
}

/// <summary>
/// Single source of truth for the human-readable blood group label, so every API response
/// carries the same "A+"/"O-" text instead of leaving Angular to guess a mapping from the
/// numeric enum value.
/// </summary>
public static class BloodGroupExtensions
{
    public static string ToDisplayName(this BloodGroup bloodGroup) => bloodGroup switch
    {
        BloodGroup.APositive => "A+",
        BloodGroup.ANegative => "A-",
        BloodGroup.BPositive => "B+",
        BloodGroup.BNegative => "B-",
        BloodGroup.ABPositive => "AB+",
        BloodGroup.ABNegative => "AB-",
        BloodGroup.OPositive => "O+",
        BloodGroup.ONegative => "O-",
        _ => bloodGroup.ToString()
    };
}
