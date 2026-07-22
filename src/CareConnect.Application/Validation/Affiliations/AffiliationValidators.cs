using CareConnect.Application.DTOs.Affiliations;
using FluentValidation;

namespace CareConnect.Application.Validation.Affiliations;

public class CreateAffiliationRequestValidator : AbstractValidator<CreateAffiliationRequest>
{
    public CreateAffiliationRequestValidator()
    {
        RuleFor(x => x.HospitalProfileId)
            .NotEqual(Guid.Empty).WithMessage("A hospital must be selected.");
    }
}

public class RejectAffiliationRequestValidator : AbstractValidator<RejectAffiliationRequest>
{
    public RejectAffiliationRequestValidator()
    {
        // A rejection always has to say why, so the doctor gets actionable feedback.
        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("A rejection reason is required.")
            .MinimumLength(5).WithMessage("Please give a rejection reason of at least 5 characters.")
            .MaximumLength(500).WithMessage("The rejection reason must not exceed 500 characters.");
    }
}
