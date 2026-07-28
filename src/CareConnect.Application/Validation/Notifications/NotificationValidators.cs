using CareConnect.Application.DTOs.Notifications;
using FluentValidation;

namespace CareConnect.Application.Validation.Notifications;

public sealed class NotificationFilterValidator : AbstractValidator<NotificationFilter>
{
    public NotificationFilterValidator()
    {
        RuleFor(x => x.Search).MaximumLength(150);
        RuleFor(x => x.PageSize).LessThanOrEqualTo(50);
        RuleFor(x => x.SortDirection)
            .Must(value => value is "asc" or "desc")
            .WithMessage("SortDirection must be 'asc' or 'desc'.");
        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
    }
}
