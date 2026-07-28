namespace CareConnect.Infrastructure.Settings;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-3.6-flash";
    public int MaxOutputTokens { get; set; } = 800;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaximumHistoryMessages { get; set; } = 10;
    public int MaximumMessageCharacters { get; set; } = 2_000;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(Model);
}
