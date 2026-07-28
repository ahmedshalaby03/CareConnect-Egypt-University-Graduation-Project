using CareConnect.Application.DTOs.MedicalServiceRequests;
using CareConnect.Domain.Enums;
using FluentValidation;

namespace CareConnect.Application.Validation.MedicalServiceRequests;

public sealed class CreateMedicalServiceRequestRequestValidator
    : AbstractValidator<CreateMedicalServiceRequestRequest>
{
    public CreateMedicalServiceRequestRequestValidator()
    {
        RuleFor(x => x.MedicalServiceOfferingId)
            .NotEqual(Guid.Empty).WithMessage("Select a medical service.");
        RuleFor(x => x.RequestedDate)
            .NotEqual(default(DateOnly)).WithMessage("Select a preferred date.");
        RuleFor(x => x.PreferredStartTime)
            .NotEmpty().WithMessage("Select a preferred start time.")
            .Must(value => TimeOnly.TryParse(value, out _))
            .WithMessage("Preferred start time is invalid.");
        RuleFor(x => x.DeliveryMode)
            .IsInEnum().WithMessage("Select a valid delivery mode.");
        RuleFor(x => x.PatientNotes)
            .MaximumLength(2000).WithMessage("Patient notes must not exceed 2000 characters.");
        RuleFor(x => x.HomeVisitAddress)
            .MaximumLength(500).WithMessage("Home-visit address must not exceed 500 characters.");
        RuleFor(x => x.HomeVisitAddress)
            .NotEmpty().WithMessage("A home-visit address is required.")
            .When(x => x.DeliveryMode == ServiceDeliveryMode.HomeVisit);
    }
}

public sealed class AcceptMedicalServiceRequestRequestValidator
    : AbstractValidator<AcceptMedicalServiceRequestRequest>
{
    public AcceptMedicalServiceRequestRequestValidator()
    {
        RuleFor(x => x.ScheduledDate)
            .NotEqual(default(DateOnly)).WithMessage("Select a confirmed date.");
        RuleFor(x => x.ScheduledStartTime)
            .NotEmpty().WithMessage("Select a confirmed start time.")
            .Must(value => TimeOnly.TryParse(value, out _))
            .WithMessage("Confirmed start time is invalid.");
        RuleFor(x => x.ProviderResponseNote)
            .MaximumLength(2000).WithMessage("Provider response must not exceed 2000 characters.");
    }
}

public sealed class RejectMedicalServiceRequestRequestValidator
    : AbstractValidator<RejectMedicalServiceRequestRequest>
{
    public RejectMedicalServiceRequestRequestValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("A rejection reason is required.")
            .MinimumLength(5).WithMessage("Please provide at least 5 characters.")
            .MaximumLength(1000).WithMessage("Rejection reason must not exceed 1000 characters.");
        RuleFor(x => x.ProviderResponseNote)
            .MaximumLength(2000).WithMessage("Provider response must not exceed 2000 characters.");
    }
}

public sealed class CancelMedicalServiceRequestRequestValidator
    : AbstractValidator<CancelMedicalServiceRequestRequest>
{
    public CancelMedicalServiceRequestRequestValidator()
    {
        RuleFor(x => x.CancellationReason)
            .NotEmpty().WithMessage("A cancellation reason is required.")
            .MinimumLength(5).WithMessage("Please provide at least 5 characters.")
            .MaximumLength(1000).WithMessage("Cancellation reason must not exceed 1000 characters.");
    }
}

public sealed class PatientMedicalServiceRequestFilterValidator
    : AbstractValidator<PatientMedicalServiceRequestFilter>
{
    public PatientMedicalServiceRequestFilterValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(MedicalServiceRequestLimits.MaximumSearchLength);
        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom <= x.DateTo)
            .WithMessage("DateFrom must be before or equal to DateTo.");
    }
}

public sealed class ProviderMedicalServiceRequestFilterValidator
    : AbstractValidator<ProviderMedicalServiceRequestFilter>
{
    public ProviderMedicalServiceRequestFilterValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(MedicalServiceRequestLimits.MaximumSearchLength);
        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom <= x.DateTo)
            .WithMessage("DateFrom must be before or equal to DateTo.");
    }
}
