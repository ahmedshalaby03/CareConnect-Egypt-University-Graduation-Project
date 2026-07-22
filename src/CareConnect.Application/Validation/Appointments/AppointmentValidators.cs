using CareConnect.Application.DTOs.Appointments;
using FluentValidation;

namespace CareConnect.Application.Validation.Appointments;

public class BookAppointmentRequestValidator : AbstractValidator<BookAppointmentRequest>
{
    public BookAppointmentRequestValidator()
    {
        RuleFor(x => x.DoctorProfileId)
            .NotEqual(Guid.Empty).WithMessage("A doctor must be selected.");

        RuleFor(x => x.HospitalProfileId)
            .NotEqual(Guid.Empty).WithMessage("A hospital must be selected.");

        RuleFor(x => x.AppointmentDate)
            .NotEqual(default(DateOnly)).WithMessage("An appointment date is required.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("A time slot must be selected.")
            .Must(value => TimeOnly.TryParse(value, out _))
            .WithMessage("The selected time slot is not valid.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Please give a reason for the visit.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");

        RuleFor(x => x.PatientNotes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");
    }
}

public class RejectAppointmentRequestValidator : AbstractValidator<RejectAppointmentRequest>
{
    public RejectAppointmentRequestValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("A rejection reason is required.")
            .MinimumLength(5).WithMessage("Please give a rejection reason of at least 5 characters.")
            .MaximumLength(500).WithMessage("The rejection reason must not exceed 500 characters.");
    }
}

public class CancelAppointmentRequestValidator : AbstractValidator<CancelAppointmentRequest>
{
    public CancelAppointmentRequestValidator()
    {
        RuleFor(x => x.CancellationReason)
            .NotEmpty().WithMessage("A cancellation reason is required.")
            .MinimumLength(5).WithMessage("Please give a cancellation reason of at least 5 characters.")
            .MaximumLength(500).WithMessage("The cancellation reason must not exceed 500 characters.");
    }
}

public class DoctorNotesRequestValidator : AbstractValidator<DoctorNotesRequest>
{
    public DoctorNotesRequestValidator()
    {
        RuleFor(x => x.DoctorNotes)
            .MaximumLength(4000).WithMessage("Doctor notes must not exceed 4000 characters.");
    }
}
