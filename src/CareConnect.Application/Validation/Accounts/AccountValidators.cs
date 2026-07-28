using CareConnect.Application.DTOs.Accounts;
using CareConnect.Application.Validation;
using FluentValidation;

namespace CareConnect.Application.Validation.Accounts;

public class UpdateAccountProfileRequestValidator : AbstractValidator<UpdateAccountProfileRequest>
{
    public UpdateAccountProfileRequestValidator()
    {
        RuleFor(request => request.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Full name cannot contain only whitespace.")
            .MinimumLength(3).WithMessage("Full name must be at least 3 characters long.")
            .MaximumLength(150).WithMessage("Full name must not exceed 150 characters.");

        RuleFor(request => request.PhoneNumber).OptionalPhoneNumber();
    }
}
