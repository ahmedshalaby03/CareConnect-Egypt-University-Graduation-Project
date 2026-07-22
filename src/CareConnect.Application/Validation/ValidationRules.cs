using System.Text.RegularExpressions;
using FluentValidation;

namespace CareConnect.Application.Validation;

public static partial class ValidationRules
{
    public const int PasswordMinimumLength = 8;

    /// <summary>Mirrors the Identity password options configured in Infrastructure.</summary>
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordMinimumLength)
                .WithMessage($"Password must be at least {PasswordMinimumLength} characters long.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

    public static IRuleBuilderOptions<T, string> EmailAddressRule<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

    /// <summary>
    /// Optional field: an empty phone number passes. Format is kept loose on purpose because
    /// Egyptian mobile, landline and short numbers all look different.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> OptionalPhoneNumber<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(phone => string.IsNullOrWhiteSpace(phone) || PhonePattern().IsMatch(phone.Trim()))
            .WithMessage("Enter a valid phone number (7-20 digits, optionally starting with +).");

    [GeneratedRegex(@"^\+?[0-9][0-9\s\-]{6,19}$")]
    private static partial Regex PhonePattern();
}
