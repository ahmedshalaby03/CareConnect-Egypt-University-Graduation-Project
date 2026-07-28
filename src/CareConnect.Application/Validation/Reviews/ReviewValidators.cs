using CareConnect.Application.DTOs.Reviews;
using FluentValidation;

namespace CareConnect.Application.Validation.Reviews;

public sealed class SaveReviewRequestValidator : AbstractValidator<SaveReviewRequest>
{
    public SaveReviewRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment)
            .MaximumLength(2000)
            .Must(comment => string.IsNullOrEmpty(comment) || !string.IsNullOrWhiteSpace(comment))
            .WithMessage("Comment cannot contain only whitespace.");
    }
}

public sealed class ModerateReviewRequestValidator : AbstractValidator<ModerateReviewRequest>
{
    public ModerateReviewRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class PatientReviewFilterValidator : AbstractValidator<PatientReviewFilter>
{
    public PatientReviewFilterValidator()
    {
        RuleFor(x => x.Search).MaximumLength(150);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating.HasValue);
        RuleFor(x => x.SortBy).Must(KnownSort).WithMessage("SortBy is invalid.");
    }

    internal static bool KnownSort(string value) =>
        value is "newest" or "oldest" or "highest-rating" or "lowest-rating";
}

public sealed class ReviewListFilterValidator : AbstractValidator<ReviewListFilter>
{
    public ReviewListFilterValidator()
    {
        RuleFor(x => x.Search).MaximumLength(150);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating.HasValue);
        RuleFor(x => x.SortBy).Must(PatientReviewFilterValidator.KnownSort)
            .WithMessage("SortBy is invalid.");
        RuleFor(x => x.DateTo).GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
    }
}

public sealed class SuperAdminReviewFilterValidator : AbstractValidator<SuperAdminReviewFilter>
{
    public SuperAdminReviewFilterValidator()
    {
        RuleFor(x => x.Search).MaximumLength(150);
        RuleFor(x => x.PatientName).MaximumLength(150);
        RuleFor(x => x.TargetName).MaximumLength(150);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating.HasValue);
        RuleFor(x => x.SortBy).Must(PatientReviewFilterValidator.KnownSort)
            .WithMessage("SortBy is invalid.");
        RuleFor(x => x.DateTo).GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
    }
}
