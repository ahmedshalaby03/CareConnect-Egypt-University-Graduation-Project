namespace CareConnect.Infrastructure.Settings;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Never commit a real value. Development reads it from user secrets or
    /// appsettings.Development.json; production should use an environment variable.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>Tolerance for clock drift between the API host and the token issuer.</summary>
    public int ClockSkewSeconds { get; set; } = 30;
}
