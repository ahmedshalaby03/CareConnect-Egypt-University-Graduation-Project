using CareConnect.Application.DTOs.Specialties;
using FluentValidation;

namespace CareConnect.Application.Validation.Specialties;

public class CreateSpecialtyRequestValidator : AbstractValidator<CreateSpecialtyRequest>
{
    public CreateSpecialtyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Specialty name is required.")
            .MinimumLength(2).WithMessage("Specialty name must be at least 2 characters long.")
            .MaximumLength(120).WithMessage("Specialty name must not exceed 120 characters.");

        RuleFor(x => x.ArabicName)
            .MaximumLength(120).WithMessage("Arabic name must not exceed 120 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}

public class UpdateSpecialtyRequestValidator : AbstractValidator<UpdateSpecialtyRequest>
{
    public UpdateSpecialtyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Specialty name is required.")
            .MinimumLength(2).WithMessage("Specialty name must be at least 2 characters long.")
            .MaximumLength(120).WithMessage("Specialty name must not exceed 120 characters.");

        RuleFor(x => x.ArabicName)
            .MaximumLength(120).WithMessage("Arabic name must not exceed 120 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
