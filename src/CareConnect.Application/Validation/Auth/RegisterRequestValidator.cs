using CareConnect.Application.DTOs.Auth;
using CareConnect.Domain.Constants;
using FluentValidation;

namespace CareConnect.Application.Validation.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(3).WithMessage("Full name must be at least 3 characters long.")
            .MaximumLength(150).WithMessage("Full name must not exceed 150 characters.");

        RuleFor(x => x.Email).EmailAddressRule();

        RuleFor(x => x.PhoneNumber).OptionalPhoneNumber();

        RuleFor(x => x.Password).Password();

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required.")
            .Equal(x => x.Password).WithMessage("Password and confirmation do not match.");

        // The client sends a role, but only these four are ever honoured. SuperAdmin is seeded, never registered.
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(AppRoles.IsPublicRole)
            .WithMessage($"Role must be one of: {string.Join(", ", AppRoles.PublicRoles)}.");
    }
}
