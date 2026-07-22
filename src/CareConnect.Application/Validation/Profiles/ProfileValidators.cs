using System.Globalization;
using CareConnect.Application.DTOs.Doctors;
using CareConnect.Application.DTOs.Hospitals;
using FluentValidation;

namespace CareConnect.Application.Validation.Profiles;

public class UpdateDoctorProfileRequestValidator : AbstractValidator<UpdateDoctorProfileRequest>
{
    public UpdateDoctorProfileRequestValidator()
    {
        // FullName and PhoneNumber are optional here: an update that leaves them out keeps
        // whatever the account already has. An empty string, however, is a mistake.
        RuleFor(x => x.FullName)
            .MinimumLength(3).WithMessage("Full name must be at least 3 characters long.")
            .MaximumLength(150).WithMessage("Full name must not exceed 150 characters.")
            .When(x => x.FullName is not null);

        RuleFor(x => x.PhoneNumber).OptionalPhoneNumber();

        RuleFor(x => x.LicenseNumber)
            .MaximumLength(100).WithMessage("License number must not exceed 100 characters.");

        RuleFor(x => x.YearsOfExperience)
            .InclusiveBetween(0, 70)
            .WithMessage("Years of experience must be between 0 and 70.")
            .When(x => x.YearsOfExperience.HasValue);

        RuleFor(x => x.ConsultationPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Consultation price cannot be negative.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Consultation price is unrealistically high.")
            .When(x => x.ConsultationPrice.HasValue);

        RuleFor(x => x.Biography)
            .MaximumLength(2000).WithMessage("Biography must not exceed 2000 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(400).WithMessage("Address must not exceed 400 characters.");

        RuleFor(x => x.Governorate)
            .MaximumLength(100).WithMessage("Governorate must not exceed 100 characters.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.ProfileImageUrl).OptionalHttpUrl("Profile image URL");
    }
}

public class UpdateHospitalProfileRequestValidator : AbstractValidator<UpdateHospitalProfileRequest>
{
    public UpdateHospitalProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MinimumLength(3).WithMessage("Account name must be at least 3 characters long.")
            .MaximumLength(150).WithMessage("Account name must not exceed 150 characters.")
            .When(x => x.FullName is not null);

        RuleFor(x => x.HospitalName)
            .MaximumLength(200).WithMessage("Hospital name must not exceed 200 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(400).WithMessage("Address must not exceed 400 characters.");

        RuleFor(x => x.Governorate)
            .MaximumLength(100).WithMessage("Governorate must not exceed 100 characters.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber).OptionalPhoneNumber();

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.LogoUrl).OptionalHttpUrl("Logo URL");
        RuleFor(x => x.WebsiteUrl).OptionalHttpUrl("Website URL");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.OpeningTime).OptionalTimeOfDay("Opening time");
        RuleFor(x => x.ClosingTime).OptionalTimeOfDay("Closing time");
    }
}

public class UpdateHospitalSpecialtiesRequestValidator : AbstractValidator<UpdateHospitalSpecialtiesRequest>
{
    public UpdateHospitalSpecialtiesRequestValidator()
    {
        RuleFor(x => x.SpecialtyIds)
            .NotNull().WithMessage("A list of specialty ids is required (send an empty list to clear).")
            .Must(ids => ids.Count <= 60)
            .WithMessage("A hospital cannot list more than 60 specialties.");

        RuleFor(x => x.SpecialtyIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("The same specialty was sent more than once.")
            .When(x => x.SpecialtyIds is not null);

        RuleForEach(x => x.SpecialtyIds)
            .NotEqual(Guid.Empty).WithMessage("A specialty id is not valid.");
    }
}

/// <summary>Small shared rules used by the profile validators above.</summary>
internal static class ProfileValidationRules
{
    public static IRuleBuilderOptions<T, string?> OptionalHttpUrl<T>(
        this IRuleBuilder<T, string?> rule,
        string fieldName) =>
        rule.Must(value =>
                string.IsNullOrWhiteSpace(value)
                || (value.Length <= 500
                    && Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage($"{fieldName} must be a valid http or https address (max 500 characters).");

    /// <summary>Accepts "HH:mm" or "HH:mm:ss", or nothing at all.</summary>
    public static IRuleBuilderOptions<T, string?> OptionalTimeOfDay<T>(
        this IRuleBuilder<T, string?> rule,
        string fieldName) =>
        rule.Must(value =>
                string.IsNullOrWhiteSpace(value)
                || TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                || TimeOnly.TryParseExact(value, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage($"{fieldName} must be in 24-hour HH:mm format.");
}
