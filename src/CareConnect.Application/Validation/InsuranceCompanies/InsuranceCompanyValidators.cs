using CareConnect.Application.DTOs.InsuranceCompanies;
using CareConnect.Application.Validation.Profiles;
using FluentValidation;
// Brings the OptionalPhoneNumber extension (declared in the parent Validation namespace)
// into scope; OptionalHttpUrl comes from the Validation.Profiles import above.
using CareConnect.Application.Validation;

namespace CareConnect.Application.Validation.InsuranceCompanies;

public class CreateInsuranceCompanyRequestValidator : AbstractValidator<CreateInsuranceCompanyRequest>
{
    public CreateInsuranceCompanyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Insurance company name is required.")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters long.")
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

        RuleFor(x => x.ArabicName)
            .MaximumLength(150).WithMessage("Arabic name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.PhoneNumber).OptionalPhoneNumber();
        RuleFor(x => x.WebsiteUrl).OptionalHttpUrl("Website URL");
        RuleFor(x => x.LogoUrl).OptionalHttpUrl("Logo URL");
    }
}

public class UpdateInsuranceCompanyRequestValidator : AbstractValidator<UpdateInsuranceCompanyRequest>
{
    public UpdateInsuranceCompanyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Insurance company name is required.")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters long.")
            .MaximumLength(150).WithMessage("Name must not exceed 150 characters.");

        RuleFor(x => x.ArabicName)
            .MaximumLength(150).WithMessage("Arabic name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.PhoneNumber).OptionalPhoneNumber();
        RuleFor(x => x.WebsiteUrl).OptionalHttpUrl("Website URL");
        RuleFor(x => x.LogoUrl).OptionalHttpUrl("Logo URL");
    }
}
