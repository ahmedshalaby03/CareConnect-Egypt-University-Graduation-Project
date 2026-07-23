using CareConnect.Application.DTOs.InsuranceRequests;
using CareConnect.Application.Validation.Profiles;
using FluentValidation;

namespace CareConnect.Application.Validation.InsuranceRequests;

public class CreateInsuranceRequestRequestValidator : AbstractValidator<CreateInsuranceRequestRequest>
{
    public CreateInsuranceRequestRequestValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEqual(Guid.Empty).WithMessage("An appointment must be selected.");

        RuleFor(x => x.InsuranceCompanyId)
            .NotEqual(Guid.Empty).WithMessage("An insurance company must be selected.");

        RuleFor(x => x.MemberNumber)
            .NotEmpty().WithMessage("Member number is required.")
            .MaximumLength(100).WithMessage("Member number must not exceed 100 characters.");

        RuleFor(x => x.PolicyNumber)
            .MaximumLength(100).WithMessage("Policy number must not exceed 100 characters.");

        RuleFor(x => x.ServiceDescription)
            .NotEmpty().WithMessage("Service description is required.")
            .MaximumLength(1000).WithMessage("Service description must not exceed 1000 characters.");

        RuleFor(x => x.RequestedAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Requested amount cannot be negative.")
            .LessThanOrEqualTo(10_000_000).WithMessage("Requested amount is unrealistically high.")
            .When(x => x.RequestedAmount.HasValue);

        RuleFor(x => x.PatientNotes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");

        RuleFor(x => x.InsuranceCardImageUrl).OptionalHttpUrl("Insurance card image URL");
        RuleFor(x => x.SupportingDocumentUrl).OptionalHttpUrl("Supporting document URL");
    }
}

public class ApproveInsuranceRequestRequestValidator : AbstractValidator<ApproveInsuranceRequestRequest>
{
    public ApproveInsuranceRequestRequestValidator()
    {
        RuleFor(x => x.ApprovedAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Approved amount cannot be negative.")
            .When(x => x.ApprovedAmount.HasValue);

        RuleFor(x => x.ApprovalReferenceNumber)
            .MaximumLength(100).WithMessage("Approval reference number must not exceed 100 characters.");

        RuleFor(x => x.HospitalNotes)
            .MaximumLength(2000).WithMessage("Hospital notes must not exceed 2000 characters.");
    }
}

public class RejectInsuranceRequestRequestValidator : AbstractValidator<RejectInsuranceRequestRequest>
{
    public RejectInsuranceRequestRequestValidator()
    {
        // A rejection always has to say why, so the patient gets actionable feedback.
        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("A rejection reason is required.")
            .MinimumLength(5).WithMessage("Please give a rejection reason of at least 5 characters.")
            .MaximumLength(500).WithMessage("The rejection reason must not exceed 500 characters.");

        RuleFor(x => x.HospitalNotes)
            .MaximumLength(2000).WithMessage("Hospital notes must not exceed 2000 characters.");
    }
}

public class InsuranceHospitalNotesRequestValidator : AbstractValidator<InsuranceHospitalNotesRequest>
{
    public InsuranceHospitalNotesRequestValidator()
    {
        RuleFor(x => x.HospitalNotes)
            .MaximumLength(2000).WithMessage("Hospital notes must not exceed 2000 characters.");
    }
}
