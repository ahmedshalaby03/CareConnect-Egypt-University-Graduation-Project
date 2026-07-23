using CareConnect.Application.DTOs.BloodStock;
using FluentValidation;

namespace CareConnect.Application.Validation.BloodStock;

public class CreateBloodStockRequestValidator : AbstractValidator<CreateBloodStockRequest>
{
    public CreateBloodStockRequestValidator()
    {
        RuleFor(x => x.BloodGroup).IsInEnum();

        RuleFor(x => x.AvailableUnits)
            .GreaterThanOrEqualTo(0).WithMessage("Available units cannot be negative.");

        RuleFor(x => x.MinimumRequiredUnits)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum required units cannot be negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
    }
}

public class UpdateBloodStockRequestValidator : AbstractValidator<UpdateBloodStockRequest>
{
    public UpdateBloodStockRequestValidator()
    {
        RuleFor(x => x.AvailableUnits)
            .GreaterThanOrEqualTo(0).WithMessage("Available units cannot be negative.");

        RuleFor(x => x.MinimumRequiredUnits)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum required units cannot be negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
    }
}

public class IncreaseBloodStockRequestValidator : AbstractValidator<IncreaseBloodStockRequest>
{
    public IncreaseBloodStockRequestValidator()
    {
        RuleFor(x => x.Units)
            .GreaterThan(0).WithMessage("Units to add must be greater than zero.")
            .LessThanOrEqualTo(10_000).WithMessage("Units to add is unrealistically high.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
    }
}

public class DecreaseBloodStockRequestValidator : AbstractValidator<DecreaseBloodStockRequest>
{
    public DecreaseBloodStockRequestValidator()
    {
        RuleFor(x => x.Units)
            .GreaterThan(0).WithMessage("Units to remove must be greater than zero.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
    }
}
