using CareConnect.Application.DTOs.Scheduling;
using FluentValidation;

namespace CareConnect.Application.Validation.Scheduling;

public class CreateUnavailablePeriodRequestValidator : AbstractValidator<CreateUnavailablePeriodRequest>
{
    public CreateUnavailablePeriodRequestValidator()
    {
        RuleFor(x => x.HospitalProfileId)
            .NotEqual(Guid.Empty).WithMessage("A hospital must be selected.");

        RuleFor(x => x.StartDateTime)
            .NotEqual(default(DateTime)).WithMessage("Start date and time is required.");

        RuleFor(x => x.EndDateTime)
            .NotEqual(default(DateTime)).WithMessage("End date and time is required.")
            .GreaterThan(x => x.StartDateTime)
            .WithMessage("End date and time must be after the start date and time.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}
