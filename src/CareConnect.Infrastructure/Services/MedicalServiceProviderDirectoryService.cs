using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceProviders;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareConnect.Infrastructure.Services;

public sealed class MedicalServiceProviderDirectoryService
    : IMedicalServiceProviderDirectoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IGeoDistanceService _geoDistance;

    public MedicalServiceProviderDirectoryService(
        ApplicationDbContext context,
        IGeoDistanceService geoDistance)
    {
        _context = context;
        _geoDistance = geoDistance;
    }

    public async Task<Result<PagedResult<MedicalServiceProviderSummaryDto>>> SearchAsync(
        MedicalServiceProviderFilter filter,
        CancellationToken ct = default)
    {
        var query = EligibleProfiles();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(profile =>
                EF.Functions.Like(profile.BusinessName!, $"%{term}%") ||
                profile.ServiceOfferings.Any(service =>
                    service.IsActive &&
                    service.MedicalServiceCategory!.IsActive &&
                    EF.Functions.Like(service.Name, $"%{term}%")));
        }

        if (filter.ProviderType.HasValue)
        {
            query = query.Where(profile => profile.ProviderType == filter.ProviderType.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(profile => profile.ServiceOfferings.Any(service =>
                service.IsActive &&
                service.MedicalServiceCategory!.IsActive &&
                service.MedicalServiceCategoryId == filter.CategoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.Governorate))
        {
            var governorate = filter.Governorate.Trim().ToLower();
            query = query.Where(profile => profile.Governorate!.ToLower() == governorate);
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var city = filter.City.Trim().ToLower();
            query = query.Where(profile => profile.City!.ToLower() == city);
        }

        var hasCoordinates = filter.Latitude.HasValue && filter.Longitude.HasValue;
        if (hasCoordinates)
        {
            var box = _geoDistance.CalculateBoundingBox(
                filter.Latitude!.Value,
                filter.Longitude!.Value,
                filter.RadiusKm);
            query = query.Where(profile =>
                profile.Latitude >= box.MinLatitude &&
                profile.Latitude <= box.MaxLatitude &&
                profile.Longitude >= box.MinLongitude &&
                profile.Longitude <= box.MaxLongitude);
        }

        var candidates = await query
            .Select(profile => new DirectoryCandidate
            {
                Id = profile.Id,
                BusinessName = profile.BusinessName!,
                ProviderType = profile.ProviderType!.Value,
                Description = profile.Description,
                Governorate = profile.Governorate,
                City = profile.City,
                Latitude = profile.Latitude!.Value,
                Longitude = profile.Longitude!.Value,
                MinimumServicePrice = profile.ServiceOfferings
                    .Where(service =>
                        service.IsActive &&
                        service.MedicalServiceCategory!.IsActive)
                    .Min(service => (decimal?)service.Price),
                Categories = profile.ServiceOfferings
                    .Where(service =>
                        service.IsActive &&
                        service.MedicalServiceCategory!.IsActive)
                    .Select(service => new MedicalServiceCategoryOptionDto
                    {
                        Id = service.MedicalServiceCategoryId,
                        Name = service.MedicalServiceCategory!.Name
                    })
                    .Distinct()
                    .OrderBy(category => category.Name)
                    .ToList()
            })
            .ToListAsync(ct);

        foreach (var candidate in candidates)
        {
            if (hasCoordinates)
            {
                candidate.DistanceKm = _geoDistance.CalculateDistanceKm(
                    filter.Latitude!.Value,
                    filter.Longitude!.Value,
                    candidate.Latitude,
                    candidate.Longitude);
            }
        }

        IEnumerable<DirectoryCandidate> filtered = candidates;
        if (hasCoordinates)
        {
            filtered = filtered.Where(candidate => candidate.DistanceKm <= filter.RadiusKm);
        }

        filtered = filter.SortBy.ToLowerInvariant() switch
        {
            "distance" => filtered.OrderBy(candidate => candidate.DistanceKm),
            "minimumprice" => filtered
                .OrderBy(candidate => candidate.MinimumServicePrice)
                .ThenBy(candidate => candidate.BusinessName),
            _ => filtered.OrderBy(candidate => candidate.BusinessName)
        };

        var sorted = filtered.ToList();
        var totalCount = sorted.Count;
        var items = sorted
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .Select(candidate => new MedicalServiceProviderSummaryDto
            {
                Id = candidate.Id,
                BusinessName = candidate.BusinessName,
                ProviderType = candidate.ProviderType,
                ProviderTypeName = candidate.ProviderType.ToString(),
                Description = candidate.Description,
                Governorate = candidate.Governorate,
                City = candidate.City,
                Categories = candidate.Categories,
                MinimumServicePrice = candidate.MinimumServicePrice,
                DistanceKm = candidate.DistanceKm
            })
            .ToList();

        return Result<PagedResult<MedicalServiceProviderSummaryDto>>.Success(
            PagedResult<MedicalServiceProviderSummaryDto>.Create(
                items,
                filter.Page,
                filter.PageSize,
                totalCount),
            "Medical service providers retrieved successfully.");
    }

    public async Task<Result<MedicalServiceProviderDetailsDto>> GetByIdAsync(
        Guid id,
        MedicalServiceProviderDetailsQuery query,
        CancellationToken ct = default)
    {
        var provider = await EligibleProfiles()
            .Include(profile => profile.ServiceOfferings)
                .ThenInclude(service => service.MedicalServiceCategory)
            .Include(profile => profile.WorkingHours)
            .Where(profile => profile.Id == id)
            .FirstOrDefaultAsync(ct);

        if (provider is null)
        {
            return Result<MedicalServiceProviderDetailsDto>.NotFound(
                "Medical service provider not found.");
        }

        var distanceKm = query.Latitude.HasValue && query.Longitude.HasValue
            ? _geoDistance.CalculateDistanceKm(
                query.Latitude.Value,
                query.Longitude.Value,
                provider.Latitude!.Value,
                provider.Longitude!.Value)
            : (double?)null;

        return Result<MedicalServiceProviderDetailsDto>.Success(
            new MedicalServiceProviderDetailsDto
            {
                Id = provider.Id,
                BusinessName = provider.BusinessName!,
                ProviderType = provider.ProviderType!.Value,
                ProviderTypeName = provider.ProviderType.Value.ToString(),
                Description = provider.Description!,
                PhoneNumber = provider.PhoneNumber!,
                Address = provider.Address!,
                Governorate = provider.Governorate!,
                City = provider.City!,
                Latitude = provider.Latitude!.Value,
                Longitude = provider.Longitude!.Value,
                DirectionsUrl = DirectionsUrlBuilder.Build(
                    provider.Latitude,
                    provider.Longitude)!,
                DistanceKm = distanceKm,
                Services = provider.ServiceOfferings
                    .Where(service =>
                        service.IsActive &&
                        service.MedicalServiceCategory?.IsActive == true)
                    .OrderBy(service => service.MedicalServiceCategory!.Name)
                    .ThenBy(service => service.Name)
                    .Select(service => new MedicalServiceOfferingDto
                    {
                        Id = service.Id,
                        CategoryId = service.MedicalServiceCategoryId,
                        CategoryName = service.MedicalServiceCategory!.Name,
                        Name = service.Name,
                        Description = service.Description,
                        Price = service.Price,
                        EstimatedDurationMinutes = service.EstimatedDurationMinutes,
                        PreparationInstructions = service.PreparationInstructions,
                        DeliveryModeAvailability = service.DeliveryModeAvailability,
                        IsActive = true,
                        CreatedAt = service.CreatedAt,
                        UpdatedAt = service.UpdatedAt
                    })
                    .ToList(),
                WorkingHours = provider.WorkingHours
                    .OrderBy(hour => DayOrder(hour.DayOfWeek))
                    .Select(hour => new MedicalServiceProviderWorkingHourDto
                    {
                        Id = hour.Id,
                        DayOfWeek = hour.DayOfWeek,
                        DayName = hour.DayOfWeek.ToString(),
                        OpenTime = hour.OpenTime?.ToString("HH:mm"),
                        CloseTime = hour.CloseTime?.ToString("HH:mm"),
                        IsClosed = hour.IsClosed
                    })
                    .ToList()
            },
            "Medical service provider retrieved successfully.");
    }

    private IQueryable<MedicalServiceProviderProfile> EligibleProfiles() =>
        _context.MedicalServiceProviderProfiles
            .AsNoTracking()
            .Where(profile =>
                profile.User!.IsActive &&
                profile.IsPublished &&
                profile.BusinessName != null &&
                profile.ProviderType != null &&
                profile.Description != null &&
                profile.PhoneNumber != null &&
                profile.Address != null &&
                profile.Governorate != null &&
                profile.City != null &&
                profile.Latitude != null &&
                profile.Longitude != null &&
                profile.ServiceOfferings.Any(service =>
                    service.IsActive &&
                    service.MedicalServiceCategory!.IsActive) &&
                profile.WorkingHours.Count == 7 &&
                profile.WorkingHours.Any(hour => !hour.IsClosed) &&
                profile.WorkingHours.All(hour =>
                    hour.IsClosed ||
                    (hour.OpenTime != null &&
                     hour.CloseTime != null &&
                     hour.OpenTime < hour.CloseTime)));

    private static int DayOrder(DayOfWeek day) =>
        day == DayOfWeek.Saturday ? 0 : (int)day + 1;

    private sealed class DirectoryCandidate
    {
        public Guid Id { get; init; }
        public string BusinessName { get; init; } = string.Empty;
        public Domain.Enums.MedicalServiceProviderType ProviderType { get; init; }
        public string? Description { get; init; }
        public string? Governorate { get; init; }
        public string? City { get; init; }
        public decimal Latitude { get; init; }
        public decimal Longitude { get; init; }
        public decimal? MinimumServicePrice { get; init; }
        public double? DistanceKm { get; set; }
        public IReadOnlyList<MedicalServiceCategoryOptionDto> Categories { get; init; } = [];
    }
}
