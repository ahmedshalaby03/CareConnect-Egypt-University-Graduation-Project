using CareConnect.Application.DTOs.Scheduling;
using FluentValidation;

namespace CareConnect.Application.Validation.Scheduling;

public class CreateAvailabilityRequestValidator : AbstractValidator<CreateAvailabilityRequest>
{
    public CreateAvailabilityRequestValidator()
    {
        RuleFor(x => x.HospitalProfileId)
            .NotEqual(Guid.Empty).WithMessage("A hospital must be selected.");

        RuleFor(x => x.DayOfWeek)
            .IsInEnum().WithMessage("Day of week is not valid.");

        RuleFor(x => x.StartTime).SchedulingTimeOfDay("Start time");
        RuleFor(x => x.EndTime).SchedulingTimeOfDay("End time");

        RuleFor(x => x.SlotDurationMinutes)
            .InclusiveBetween(10, 180)
            .WithMessage("Slot duration must be between 10 and 180 minutes.");

        RuleFor(x => x)
            .Must(x => !SchedulingValidationRules.TryParseTimeRange(x.StartTime, x.EndTime, out _, out _)
                       || SchedulingValidationRules.IsOrdered(x.StartTime, x.EndTime))
            .WithMessage("Start time must be earlier than end time.")
            .WithName("StartTime");
    }
}

public class UpdateAvailabilityRequestValidator : AbstractValidator<UpdateAvailabilityRequest>
{
    public UpdateAvailabilityRequestValidator()
    {
        RuleFor(x => x.HospitalProfileId)
            .NotEqual(Guid.Empty).WithMessage("A hospital must be selected.");

        RuleFor(x => x.DayOfWeek)
            .IsInEnum().WithMessage("Day of week is not valid.");

        RuleFor(x => x.StartTime).SchedulingTimeOfDay("Start time");
        RuleFor(x => x.EndTime).SchedulingTimeOfDay("End time");

        RuleFor(x => x.SlotDurationMinutes)
            .InclusiveBetween(10, 180)
            .WithMessage("Slot duration must be between 10 and 180 minutes.");

        RuleFor(x => x)
            .Must(x => !SchedulingValidationRules.TryParseTimeRange(x.StartTime, x.EndTime, out _, out _)
                       || SchedulingValidationRules.IsOrdered(x.StartTime, x.EndTime))
            .WithMessage("Start time must be earlier than end time.")
            .WithName("StartTime");
    }
}

/// <summary>Time-of-day parsing shared by the scheduling validators.</summary>
internal static class SchedulingValidationRules
{
    public static IRuleBuilderOptions<T, string> SchedulingTimeOfDay<T>(
        this IRuleBuilder<T, string> rule,
        string fieldName) =>
        rule.NotEmpty().WithMessage($"{fieldName} is required.")
            .Must(value => TimeOnly.TryParse(value, out _))
            .WithMessage($"{fieldName} must be a valid time (HH:mm).");

    public static bool TryParseTimeRange(string start, string end, out TimeOnly startTime, out TimeOnly endTime)
    {
        startTime = default;
        endTime = default;
        return TimeOnly.TryParse(start, out startTime) && TimeOnly.TryParse(end, out endTime);
    }

    public static bool IsOrdered(string start, string end) =>
        TryParseTimeRange(start, end, out var startTime, out var endTime) && startTime < endTime;
}
