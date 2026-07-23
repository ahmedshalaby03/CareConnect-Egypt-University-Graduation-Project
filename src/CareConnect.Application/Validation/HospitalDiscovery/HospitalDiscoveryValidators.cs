using CareConnect.Application.DTOs.Directory;
using CareConnect.Application.DTOs.HospitalDiscovery;
using FluentValidation;

namespace CareConnect.Application.Validation.HospitalDiscovery;

public class NearbyHospitalQueryParametersValidator : AbstractValidator<NearbyHospitalQueryParameters>
{
    public NearbyHospitalQueryParametersValidator()
    {
        RuleFor(x => x.Latitude)
            .NotNull().WithMessage("Latitude is required for a nearby search.")
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .NotNull().WithMessage("Longitude is required for a nearby search.")
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.RadiusKm)
            .InclusiveBetween(1, 200).WithMessage("Radius must be between 1 and 200 km.");
    }
}

public class HospitalLocationDetailsQueryParametersValidator
    : AbstractValidator<HospitalLocationDetailsQueryParameters>
{
    public HospitalLocationDetailsQueryParametersValidator()
    {
        RuleFor(x => x.UserLatitude)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.UserLatitude.HasValue);

        RuleFor(x => x.UserLongitude)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.UserLongitude.HasValue);

        // Half a coordinate pair cannot produce a distance - reject rather than silently ignore it.
        RuleFor(x => x)
            .Must(x => x.UserLatitude.HasValue == x.UserLongitude.HasValue)
            .WithMessage("Both UserLatitude and UserLongitude are required to calculate distance.")
            .WithName("Coordinates");
    }
}

public class HospitalDirectoryQueryParametersValidator : AbstractValidator<HospitalDirectoryQueryParameters>
{
    public HospitalDirectoryQueryParametersValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .WithMessage("Latitude and longitude must both be provided, or both left empty.")
            .WithName("Coordinates");

        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue && x.Longitude.HasValue)
            .WithMessage("Sorting by distance requires both Latitude and Longitude.")
            .WithName("SortBy")
            .When(x => x.SortBy == HospitalSortBy.Distance);
    }
}
