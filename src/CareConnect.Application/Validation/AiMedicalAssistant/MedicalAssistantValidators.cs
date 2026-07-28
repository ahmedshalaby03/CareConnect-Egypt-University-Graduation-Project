using CareConnect.Application.DTOs.AiMedicalAssistant;
using FluentValidation;

namespace CareConnect.Application.Validation.AiMedicalAssistant;

internal sealed class MedicalAssistantHistoryItemValidator
    : AbstractValidator<MedicalAssistantHistoryItem>
{
    private static readonly string[] AllowedRoles = ["user", "assistant"];

    public MedicalAssistantHistoryItemValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => AllowedRoles.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("History role must be either 'user' or 'assistant'.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(content => !string.IsNullOrWhiteSpace(content))
            .WithMessage("History content cannot be empty.")
            .MaximumLength(MedicalAssistantLimits.MaximumMessageCharacters);
    }
}

internal sealed class MedicalAssistantChatRequestValidator
    : AbstractValidator<MedicalAssistantChatRequest>
{
    public MedicalAssistantChatRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .Must(message => !string.IsNullOrWhiteSpace(message))
            .WithMessage("Message cannot be empty.")
            .MaximumLength(MedicalAssistantLimits.MaximumMessageCharacters);

        RuleFor(x => x.History)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(history => history.Count <= MedicalAssistantLimits.MaximumHistoryMessages)
            .WithMessage(
                $"Conversation history cannot contain more than {MedicalAssistantLimits.MaximumHistoryMessages} messages.");

        RuleForEach(x => x.History)
            .SetValidator(new MedicalAssistantHistoryItemValidator());
    }
}
