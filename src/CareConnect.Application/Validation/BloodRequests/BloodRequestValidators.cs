using CareConnect.Application.DTOs.BloodRequests;
using FluentValidation;

namespace CareConnect.Application.Validation.BloodRequests;

public class CreateBloodRequestRequestValidator : AbstractValidator<CreateBloodRequestRequest>
{
    public CreateBloodRequestRequestValidator()
    {
        RuleFor(x => x.HospitalProfileId)
            .NotEqual(Guid.Empty).WithMessage("A hospital must be selected.");

        RuleFor(x => x.BloodGroup).IsInEnum();
        RuleFor(x => x.Urgency).IsInEnum();

        RuleFor(x => x.UnitsRequested)
            .InclusiveBetween(1, 20).WithMessage("Units requested must be between 1 and 20.");

        RuleFor(x => x.BeneficiaryName)
            .NotEmpty().WithMessage("Beneficiary name is required.")
            .MaximumLength(150).WithMessage("Beneficiary name must not exceed 150 characters.");

        RuleFor(x => x.BeneficiaryAge)
            .GreaterThanOrEqualTo(0).WithMessage("Beneficiary age cannot be negative.")
            .LessThanOrEqualTo(120).WithMessage("Enter a realistic beneficiary age.")
            .When(x => x.BeneficiaryAge.HasValue);

        // Format is kept loose on purpose - Egyptian mobile and landline numbers look different.
        RuleFor(x => x.ContactPhoneNumber)
            .NotEmpty().WithMessage("A contact phone number is required.")
            .Matches(@"^\+?[0-9][0-9\s\-]{6,19}$")
                .WithMessage("Enter a valid phone number (7-20 digits, optionally starting with +).");

        RuleFor(x => x.MedicalCondition)
            .MaximumLength(500).WithMessage("Medical condition must not exceed 500 characters.");

        RuleFor(x => x.HospitalOrFacilityName)
            .MaximumLength(200).WithMessage("Hospital or facility name must not exceed 200 characters.");

        RuleFor(x => x.RequestNotes)
            .MaximumLength(1000).WithMessage("Request notes must not exceed 1000 characters.");
    }
}

public class ApproveBloodRequestRequestValidator : AbstractValidator<ApproveBloodRequestRequest>
{
    public ApproveBloodRequestRequestValidator()
    {
        RuleFor(x => x.HospitalNotes)
            .MaximumLength(1000).WithMessage("Hospital notes must not exceed 1000 characters.");
    }
}

public class RejectBloodRequestRequestValidator : AbstractValidator<RejectBloodRequestRequest>
{
    public RejectBloodRequestRequestValidator()
    {
        // A rejection always has to say why, so the patient gets actionable feedback.
        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("A rejection reason is required.")
            .MinimumLength(5).WithMessage("Please give a rejection reason of at least 5 characters.")
            .MaximumLength(500).WithMessage("The rejection reason must not exceed 500 characters.");

        RuleFor(x => x.HospitalNotes)
            .MaximumLength(1000).WithMessage("Hospital notes must not exceed 1000 characters.");
    }
}

public class BloodRequestHospitalNotesRequestValidator : AbstractValidator<BloodRequestHospitalNotesRequest>
{
    public BloodRequestHospitalNotesRequestValidator()
    {
        RuleFor(x => x.HospitalNotes)
            .MaximumLength(1000).WithMessage("Hospital notes must not exceed 1000 characters.");
    }
}
