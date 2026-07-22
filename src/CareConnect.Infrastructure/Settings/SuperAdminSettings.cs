namespace CareConnect.Infrastructure.Settings;

public class SuperAdminSettings
{
    public const string SectionName = "SuperAdmin";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = "CareConnect Administrator";
    public string? PhoneNumber { get; set; }
}
