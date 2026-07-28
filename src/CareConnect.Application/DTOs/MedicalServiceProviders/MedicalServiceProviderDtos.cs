using CareConnect.Application.Common.Models;
using CareConnect.Domain.Enums;

namespace CareConnect.Application.DTOs.MedicalServiceProviders;

public static class MedicalServiceProviderLimits
{
    public const int MaximumDirectorySearchLength = 150;
    public const double DefaultRadiusKm = 25;
    public const double MaximumRadiusKm = 200;
    public const decimal MaximumServicePrice = 10_000_000m;
}

// ---------------------------------------------------------------- Categories

public class MedicalServiceCategoryOptionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public class MedicalServiceCategoryDto : MedicalServiceCategoryOptionDto
{
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int ServiceUsageCount { get; init; }
}

public class CreateMedicalServiceCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateMedicalServiceCategoryRequest : CreateMedicalServiceCategoryRequest
{
}

public class MedicalServiceCategoryQueryParameters : PagedQueryParameters
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public class SetMedicalServiceCategoryStatusRequest
{
    public bool IsActive { get; set; }
}

// ------------------------------------------------------------------ Services

public class MedicalServiceOfferingDto
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int? EstimatedDurationMinutes { get; init; }
    public string? PreparationInstructions { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class CreateMedicalServiceOfferingRequest
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? PreparationInstructions { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateMedicalServiceOfferingRequest : CreateMedicalServiceOfferingRequest
{
}

public class SetMedicalServiceOfferingStatusRequest
{
    public bool IsActive { get; set; }
}

// -------------------------------------------------------------- Working hours

public class MedicalServiceProviderWorkingHourDto
{
    public Guid Id { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public string DayName { get; init; } = string.Empty;
    public string? OpenTime { get; init; }
    public string? CloseTime { get; init; }
    public bool IsClosed { get; init; }
}

public class WorkingHourItemRequest
{
    public DayOfWeek DayOfWeek { get; set; }
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
    public bool IsClosed { get; set; }
}

public class UpdateMedicalServiceProviderWorkingHoursRequest
{
    public IReadOnlyList<WorkingHourItemRequest> WorkingHours { get; set; } = [];
}

// ------------------------------------------------------------------- Profile

public class UpdateMedicalServiceProviderProfileRequest
{
    public string BusinessName { get; set; } = string.Empty;
    public MedicalServiceProviderType? ProviderType { get; set; }
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class PublishMedicalServiceProviderProfileRequest
{
    public bool IsPublished { get; set; }
}

public class MedicalServiceProviderProfileDto
{
    public Guid Id { get; init; }
    public string? BusinessName { get; init; }
    public MedicalServiceProviderType? ProviderType { get; init; }
    public string? ProviderTypeName { get; init; }
    public string? Description { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public bool IsPublished { get; init; }
    public bool IsReadyToPublish { get; init; }
    public IReadOnlyList<string> MissingRequirements { get; init; } = [];
    public int ActiveServicesCount { get; init; }
    public int InactiveServicesCount { get; init; }
    public int ServiceCategoriesCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class MedicalServiceProviderPreviewDto
{
    public MedicalServiceProviderProfileDto Profile { get; init; } = new();
    public IReadOnlyList<MedicalServiceOfferingDto> Services { get; init; } = [];
    public IReadOnlyList<MedicalServiceProviderWorkingHourDto> WorkingHours { get; init; } = [];
    public string? DirectionsUrl { get; init; }
}

// ---------------------------------------------------------------- Directory

public class MedicalServiceProviderFilter : PagedQueryParameters
{
    public string? Search { get; set; }
    public MedicalServiceProviderType? ProviderType { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public double RadiusKm { get; set; } = MedicalServiceProviderLimits.DefaultRadiusKm;
    public string SortBy { get; set; } = "name";
}

public class MedicalServiceProviderSummaryDto
{
    public Guid Id { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public MedicalServiceProviderType ProviderType { get; init; }
    public string ProviderTypeName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public IReadOnlyList<MedicalServiceCategoryOptionDto> Categories { get; init; } = [];
    public decimal? MinimumServicePrice { get; init; }
    public double? DistanceKm { get; init; }
}

public class MedicalServiceProviderDetailsDto
{
    public Guid Id { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public MedicalServiceProviderType ProviderType { get; init; }
    public string ProviderTypeName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Governorate { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public string DirectionsUrl { get; init; } = string.Empty;
    public double? DistanceKm { get; init; }
    public IReadOnlyList<MedicalServiceOfferingDto> Services { get; init; } = [];
    public IReadOnlyList<MedicalServiceProviderWorkingHourDto> WorkingHours { get; init; } = [];
}

public class MedicalServiceProviderDetailsQuery
{
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
