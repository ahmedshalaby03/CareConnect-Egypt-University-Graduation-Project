using System.Globalization;
using CareConnect.Application.DTOs.MedicalServiceProviders;
using FluentValidation;

namespace CareConnect.Application.Validation.MedicalServiceProviders;

public sealed class CreateMedicalServiceCategoryRequestValidator
    : AbstractValidator<CreateMedicalServiceCategoryRequest>
{
    public CreateMedicalServiceCategoryRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(120).WithMessage("Category name must not exceed 120 characters.");
        RuleFor(request => request.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
    }
}

public sealed class UpdateMedicalServiceCategoryRequestValidator
    : AbstractValidator<UpdateMedicalServiceCategoryRequest>
{
    public UpdateMedicalServiceCategoryRequestValidator()
    {
        Include(new CreateMedicalServiceCategoryRequestValidator());
    }
}

public sealed class UpdateMedicalServiceProviderProfileRequestValidator
    : AbstractValidator<UpdateMedicalServiceProviderProfileRequest>
{
    public UpdateMedicalServiceProviderProfileRequestValidator()
    {
        RuleFor(request => request.BusinessName)
            .NotEmpty().WithMessage("Business name is required.")
            .MaximumLength(150).WithMessage("Business name must not exceed 150 characters.");
        RuleFor(request => request.ProviderType)
            .IsInEnum().WithMessage("Select a valid provider type.")
            .When(request => request.ProviderType.HasValue);
        RuleFor(request => request.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");
        RuleFor(request => request.PhoneNumber)
            .OptionalPhoneNumber()
            .MaximumLength(30).WithMessage("Phone number must not exceed 30 characters.");
        RuleFor(request => request.Address)
            .MaximumLength(300).WithMessage("Address must not exceed 300 characters.");
        RuleFor(request => request.Governorate)
            .MaximumLength(100).WithMessage("Governorate must not exceed 100 characters.");
        RuleFor(request => request.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");
        RuleFor(request => request.Latitude)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90.")
            .When(request => request.Latitude.HasValue);
        RuleFor(request => request.Longitude)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180.")
            .When(request => request.Longitude.HasValue);
        RuleFor(request => request)
            .Must(request => request.Latitude.HasValue == request.Longitude.HasValue)
            .WithMessage("Latitude and longitude must both be provided, or both left empty.")
            .WithName("Coordinates");
    }
}

public sealed class CreateMedicalServiceOfferingRequestValidator
    : AbstractValidator<CreateMedicalServiceOfferingRequest>
{
    public CreateMedicalServiceOfferingRequestValidator()
    {
        RuleFor(request => request.CategoryId)
            .NotEqual(Guid.Empty).WithMessage("Select a valid service category.");
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Service name is required.")
            .MaximumLength(150).WithMessage("Service name must not exceed 150 characters.");
        RuleFor(request => request.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");
        RuleFor(request => request.Price)
            .InclusiveBetween(0, MedicalServiceProviderLimits.MaximumServicePrice)
            .WithMessage($"Price must be between 0 and {MedicalServiceProviderLimits.MaximumServicePrice}.");
        RuleFor(request => request.EstimatedDurationMinutes)
            .InclusiveBetween(5, 1440)
            .WithMessage("Estimated duration must be between 5 and 1440 minutes.")
            .When(request => request.EstimatedDurationMinutes.HasValue);
        RuleFor(request => request.PreparationInstructions)
            .MaximumLength(2000)
            .WithMessage("Preparation instructions must not exceed 2000 characters.");
    }
}

public sealed class UpdateMedicalServiceOfferingRequestValidator
    : AbstractValidator<UpdateMedicalServiceOfferingRequest>
{
    public UpdateMedicalServiceOfferingRequestValidator()
    {
        Include(new CreateMedicalServiceOfferingRequestValidator());
    }
}

public sealed class UpdateMedicalServiceProviderWorkingHoursRequestValidator
    : AbstractValidator<UpdateMedicalServiceProviderWorkingHoursRequest>
{
    public UpdateMedicalServiceProviderWorkingHoursRequestValidator()
    {
        RuleFor(request => request.WorkingHours)
            .NotNull().WithMessage("Working hours are required.")
            .Must(hours => hours.Count == 7)
            .WithMessage("Working hours must contain exactly one entry for every day.")
            .Must(hours => hours.Select(item => item.DayOfWeek).Distinct().Count() == 7)
            .WithMessage("Each day may appear only once.");

        RuleForEach(request => request.WorkingHours)
            .SetValidator(new WorkingHourItemRequestValidator());
    }
}

public sealed class WorkingHourItemRequestValidator : AbstractValidator<WorkingHourItemRequest>
{
    public WorkingHourItemRequestValidator()
    {
        RuleFor(item => item.DayOfWeek).IsInEnum().WithMessage("Select a valid day.");
        RuleFor(item => item)
            .Must(HaveValidTimes)
            .WithMessage(
                "Open and close times are required for open days, and opening time must be before closing time.");
    }

    private static bool HaveValidTimes(WorkingHourItemRequest item)
    {
        if (item.IsClosed)
        {
            return string.IsNullOrWhiteSpace(item.OpenTime) &&
                   string.IsNullOrWhiteSpace(item.CloseTime);
        }

        return TryParse(item.OpenTime, out var open) &&
               TryParse(item.CloseTime, out var close) &&
               open < close;
    }

    private static bool TryParse(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time) ||
        TimeOnly.TryParseExact(value, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
}

public sealed class MedicalServiceProviderFilterValidator
    : AbstractValidator<MedicalServiceProviderFilter>
{
    private static readonly string[] SortValues = ["name", "distance", "minimumPrice"];

    public MedicalServiceProviderFilterValidator()
    {
        RuleFor(filter => filter.Search)
            .MaximumLength(MedicalServiceProviderLimits.MaximumDirectorySearchLength)
            .WithMessage(
                $"Search must not exceed {MedicalServiceProviderLimits.MaximumDirectorySearchLength} characters.");
        RuleFor(filter => filter.ProviderType)
            .IsInEnum().WithMessage("Select a valid provider type.")
            .When(filter => filter.ProviderType.HasValue);
        RuleFor(filter => filter.Latitude)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90.")
            .When(filter => filter.Latitude.HasValue);
        RuleFor(filter => filter.Longitude)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180.")
            .When(filter => filter.Longitude.HasValue);
        RuleFor(filter => filter)
            .Must(filter => filter.Latitude.HasValue == filter.Longitude.HasValue)
            .WithMessage("Latitude and longitude must both be provided, or both left empty.")
            .WithName("Coordinates");
        RuleFor(filter => filter.RadiusKm)
            .InclusiveBetween(1, MedicalServiceProviderLimits.MaximumRadiusKm)
            .WithMessage(
                $"Radius must be between 1 and {MedicalServiceProviderLimits.MaximumRadiusKm} kilometres.");
        RuleFor(filter => filter.SortBy)
            .Must(value => SortValues.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortBy must be name, distance or minimumPrice.");
        RuleFor(filter => filter)
            .Must(filter =>
                !filter.SortBy.Equals("distance", StringComparison.OrdinalIgnoreCase) ||
                (filter.Latitude.HasValue && filter.Longitude.HasValue))
            .WithMessage("Distance sorting requires both latitude and longitude.")
            .WithName("SortBy");
    }
}

public sealed class MedicalServiceProviderDetailsQueryValidator
    : AbstractValidator<MedicalServiceProviderDetailsQuery>
{
    public MedicalServiceProviderDetailsQueryValidator()
    {
        RuleFor(query => query.Latitude)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90.")
            .When(query => query.Latitude.HasValue);
        RuleFor(query => query.Longitude)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180.")
            .When(query => query.Longitude.HasValue);
        RuleFor(query => query)
            .Must(query => query.Latitude.HasValue == query.Longitude.HasValue)
            .WithMessage("Latitude and longitude must both be provided, or both left empty.")
            .WithName("Coordinates");
    }
}
