namespace CareConnect.Application.DTOs.AiMedicalAssistant;

public static class MedicalAssistantLimits
{
    public const int MaximumMessageCharacters = 2_000;
    public const int MaximumHistoryMessages = 10;
    public const int MaximumAnswerCharacters = 6_000;
    public const int MaximumDisclaimerCharacters = 500;
    public const int MaximumRedFlags = 8;
    public const int MaximumRedFlagCharacters = 300;
}

public class MedicalAssistantHistoryItem
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class MedicalAssistantChatRequest
{
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<MedicalAssistantHistoryItem> History { get; set; } = [];
}

public class MedicalAssistantChatResponse
{
    public string Answer { get; init; } = string.Empty;
    public string UrgencyLevel { get; init; } = string.Empty;
    public Guid? SuggestedSpecialtyId { get; init; }
    public string? SuggestedSpecialtyName { get; init; }
    public IReadOnlyList<string> RedFlags { get; init; } = [];
    public string Disclaimer { get; init; } = string.Empty;
}
