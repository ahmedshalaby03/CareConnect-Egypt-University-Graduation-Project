using System.Globalization;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceProviders;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

public sealed class MedicalServiceProviderProfileService
    : IMedicalServiceProviderProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicalServiceProviderProfileService> _logger;

    public MedicalServiceProviderProfileService(
        ApplicationDbContext context,
        ILogger<MedicalServiceProviderProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<MedicalServiceProviderProfileDto>> GetProfileAsync(
        string userId,
        CancellationToken ct = default)
    {
        var profile = await LoadProfileAsync(userId, false, ct);
        return profile is null
            ? Result<MedicalServiceProviderProfileDto>.NotFound(
                "Medical service provider profile not found for the current account.")
            : Result<MedicalServiceProviderProfileDto>.Success(
                ToProfileDto(profile),
                "Provider profile retrieved successfully.");
    }

    public async Task<Result<MedicalServiceProviderProfileDto>> UpdateProfileAsync(
        string userId,
        UpdateMedicalServiceProviderProfileRequest request,
        CancellationToken ct = default)
    {
        var profile = await LoadProfileAsync(userId, true, ct);
        if (profile is null)
        {
            return Result<MedicalServiceProviderProfileDto>.NotFound(
                "Medical service provider profile not found for the current account.");
        }

        profile.BusinessName = request.BusinessName.Trim();
        profile.ProviderType = request.ProviderType;
        profile.Description = Normalise(request.Description);
        profile.PhoneNumber = Normalise(request.PhoneNumber);
        profile.Address = Normalise(request.Address);
        profile.Governorate = Normalise(request.Governorate);
        profile.City = Normalise(request.City);
        profile.Latitude = request.Latitude;
        profile.Longitude = request.Longitude;
        profile.UpdatedAt = DateTime.UtcNow;
        UnpublishIfIncomplete(profile);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Medical service provider {UserId} updated its own profile.",
            userId);

        return Result<MedicalServiceProviderProfileDto>.Success(
            ToProfileDto(profile),
            "Provider profile updated successfully.");
    }

    public async Task<Result<MedicalServiceProviderProfileDto>> SetPublicationAsync(
        string userId,
        PublishMedicalServiceProviderProfileRequest request,
        CancellationToken ct = default)
    {
        var profile = await LoadProfileAsync(userId, true, ct);
        if (profile is null)
        {
            return Result<MedicalServiceProviderProfileDto>.NotFound(
                "Medical service provider profile not found for the current account.");
        }

        var missing = MissingRequirements(profile);
        if (request.IsPublished && missing.Count > 0)
        {
            return Result<MedicalServiceProviderProfileDto>.Invalid(
                "Complete the provider profile before publishing it.",
                missing);
        }

        profile.IsPublished = request.IsPublished;
        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Medical service provider {UserId} set publication to {IsPublished}.",
            userId,
            profile.IsPublished);

        return Result<MedicalServiceProviderProfileDto>.Success(
            ToProfileDto(profile),
            profile.IsPublished
                ? "Provider profile published successfully."
                : "Provider profile unpublished successfully.");
    }

    public async Task<Result<MedicalServiceProviderPreviewDto>> GetPreviewAsync(
        string userId,
        CancellationToken ct = default)
    {
        var profile = await LoadProfileAsync(userId, false, ct);
        if (profile is null)
        {
            return Result<MedicalServiceProviderPreviewDto>.NotFound(
                "Medical service provider profile not found for the current account.");
        }

        return Result<MedicalServiceProviderPreviewDto>.Success(
            new MedicalServiceProviderPreviewDto
            {
                Profile = ToProfileDto(profile),
                Services = profile.ServiceOfferings
                    .OrderBy(service => service.MedicalServiceCategory!.Name)
                    .ThenBy(service => service.Name)
                    .Select(ToServiceDto)
                    .ToList(),
                WorkingHours = profile.WorkingHours
                    .OrderBy(hour => DayOrder(hour.DayOfWeek))
                    .Select(ToWorkingHourDto)
                    .ToList(),
                DirectionsUrl = DirectionsUrlBuilder.Build(profile.Latitude, profile.Longitude)
            },
            "Provider preview retrieved successfully.");
    }

    public async Task<Result<IReadOnlyList<MedicalServiceOfferingDto>>> GetServicesAsync(
        string userId,
        CancellationToken ct = default)
    {
        var profileId = await ResolveProfileIdAsync(userId, ct);
        if (!profileId.HasValue)
        {
            return Result<IReadOnlyList<MedicalServiceOfferingDto>>.NotFound(
                "Medical service provider profile not found for the current account.");
        }

        var services = await _context.MedicalServiceOfferings
            .AsNoTracking()
            .Where(service => service.MedicalServiceProviderProfileId == profileId.Value)
            .OrderBy(service => service.Name)
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
                IsActive = service.IsActive,
                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt
            })
            .ToListAsync(ct);

        return Result<IReadOnlyList<MedicalServiceOfferingDto>>.Success(
            services,
            "Services retrieved successfully.");
    }

    public async Task<Result<MedicalServiceOfferingDto>> GetServiceAsync(
        string userId,
        Guid serviceId,
        CancellationToken ct = default)
    {
        var service = await LoadOwnedServiceAsync(userId, serviceId, false, ct);
        return service is null
            ? Result<MedicalServiceOfferingDto>.NotFound("Service not found.")
            : Result<MedicalServiceOfferingDto>.Success(
                ToServiceDto(service),
                "Service retrieved successfully.");
    }

    public async Task<Result<MedicalServiceOfferingDto>> CreateServiceAsync(
        string userId,
        CreateMedicalServiceOfferingRequest request,
        CancellationToken ct = default)
    {
        var profile = await LoadProfileAsync(userId, true, ct);
        if (profile is null)
        {
            return Result<MedicalServiceOfferingDto>.NotFound(
                "Medical service provider profile not found for the current account.");
        }

        var category = await _context.MedicalServiceCategories
            .FirstOrDefaultAsync(
                item => item.Id == request.CategoryId && item.IsActive,
                ct);
        if (category is null)
        {
            return Result<MedicalServiceOfferingDto>.Invalid(
                "Select an active medical service category.");
        }

        var name = request.Name.Trim();
        if (await ServiceNameExistsAsync(profile.Id, name, null, ct))
        {
            return Result<MedicalServiceOfferingDto>.Conflict(
                $"A service named '{name}' already exists in this provider catalog.");
        }

        var now = DateTime.UtcNow;
        var service = new MedicalServiceOffering
        {
            MedicalServiceProviderProfileId = profile.Id,
            MedicalServiceCategoryId = category.Id,
            MedicalServiceCategory = category,
            Name = name,
            Description = Normalise(request.Description),
            Price = request.Price,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            PreparationInstructions = Normalise(request.PreparationInstructions),
            DeliveryModeAvailability = request.DeliveryModeAvailability,
            IsActive = request.IsActive,
            CreatedAt = now
        };

        _context.MedicalServiceOfferings.Add(service);
        profile.UpdatedAt = now;
        await _context.SaveChangesAsync(ct);

        return Result<MedicalServiceOfferingDto>.Success(
            ToServiceDto(service),
            "Service created successfully.");
    }

    public async Task<Result<MedicalServiceOfferingDto>> UpdateServiceAsync(
        string userId,
        Guid serviceId,
        UpdateMedicalServiceOfferingRequest request,
        CancellationToken ct = default)
    {
        var service = await LoadOwnedServiceAsync(userId, serviceId, true, ct);
        if (service is null)
        {
            return Result<MedicalServiceOfferingDto>.NotFound("Service not found.");
        }

        var category = await _context.MedicalServiceCategories
            .FirstOrDefaultAsync(
                item => item.Id == request.CategoryId && item.IsActive,
                ct);
        if (category is null)
        {
            return Result<MedicalServiceOfferingDto>.Invalid(
                "Select an active medical service category.");
        }

        var name = request.Name.Trim();
        if (await ServiceNameExistsAsync(
                service.MedicalServiceProviderProfileId,
                name,
                service.Id,
                ct))
        {
            return Result<MedicalServiceOfferingDto>.Conflict(
                $"A service named '{name}' already exists in this provider catalog.");
        }

        service.MedicalServiceCategoryId = category.Id;
        service.MedicalServiceCategory = category;
        service.Name = name;
        service.Description = Normalise(request.Description);
        service.Price = request.Price;
        service.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        service.PreparationInstructions = Normalise(request.PreparationInstructions);
        service.DeliveryModeAvailability = request.DeliveryModeAvailability;
        service.IsActive = request.IsActive;
        service.UpdatedAt = DateTime.UtcNow;

        var profile = service.MedicalServiceProviderProfile!;
        profile.UpdatedAt = service.UpdatedAt;
        await _context.SaveChangesAsync(ct);
        await ReloadReadinessAsync(profile, ct);
        UnpublishIfIncomplete(profile);
        await _context.SaveChangesAsync(ct);

        return Result<MedicalServiceOfferingDto>.Success(
            ToServiceDto(service),
            "Service updated successfully.");
    }

    public async Task<Result<MedicalServiceOfferingDto>> SetServiceStatusAsync(
        string userId,
        Guid serviceId,
        SetMedicalServiceOfferingStatusRequest request,
        CancellationToken ct = default)
    {
        var service = await LoadOwnedServiceAsync(userId, serviceId, true, ct);
        if (service is null)
        {
            return Result<MedicalServiceOfferingDto>.NotFound("Service not found.");
        }

        if (request.IsActive && service.MedicalServiceCategory?.IsActive != true)
        {
            return Result<MedicalServiceOfferingDto>.Invalid(
                "This service cannot be activated while its category is inactive.");
        }

        service.IsActive = request.IsActive;
        service.UpdatedAt = DateTime.UtcNow;
        var profile = service.MedicalServiceProviderProfile!;
        profile.UpdatedAt = service.UpdatedAt;

        await _context.SaveChangesAsync(ct);
        await ReloadReadinessAsync(profile, ct);
        UnpublishIfIncomplete(profile);
        await _context.SaveChangesAsync(ct);

        return Result<MedicalServiceOfferingDto>.Success(
            ToServiceDto(service),
            service.IsActive
                ? "Service activated successfully."
                : "Service deactivated successfully.");
    }

    public async Task<Result<IReadOnlyList<MedicalServiceProviderWorkingHourDto>>> GetWorkingHoursAsync(
        string userId,
        CancellationToken ct = default)
    {
        var profileId = await ResolveProfileIdAsync(userId, ct);
        if (!profileId.HasValue)
        {
            return Result<IReadOnlyList<MedicalServiceProviderWorkingHourDto>>.NotFound(
                "Medical service provider profile not found for the current account.");
        }

        var rows = await _context.MedicalServiceProviderWorkingHours
            .AsNoTracking()
            .Where(hour => hour.MedicalServiceProviderProfileId == profileId.Value)
            .ToListAsync(ct);

        var byDay = rows.ToDictionary(hour => hour.DayOfWeek);
        var result = Enum.GetValues<DayOfWeek>()
            .OrderBy(DayOrder)
            .Select(day => byDay.TryGetValue(day, out var hour)
                ? ToWorkingHourDto(hour)
                : new MedicalServiceProviderWorkingHourDto
                {
                    DayOfWeek = day,
                    DayName = day.ToString(),
                    IsClosed = true
                })
            .ToList();

        return Result<IReadOnlyList<MedicalServiceProviderWorkingHourDto>>.Success(
            result,
            "Working hours retrieved successfully.");
    }

    public async Task<Result<IReadOnlyList<MedicalServiceProviderWorkingHourDto>>> UpdateWorkingHoursAsync(
        string userId,
        UpdateMedicalServiceProviderWorkingHoursRequest request,
        CancellationToken ct = default)
    {
        var profile = await LoadProfileAsync(userId, true, ct);
        if (profile is null)
        {
            return Result<IReadOnlyList<MedicalServiceProviderWorkingHourDto>>.NotFound(
                "Medical service provider profile not found for the current account.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var existing = profile.WorkingHours.ToDictionary(hour => hour.DayOfWeek);

        foreach (var item in request.WorkingHours)
        {
            if (!existing.TryGetValue(item.DayOfWeek, out var hour))
            {
                hour = new MedicalServiceProviderWorkingHour
                {
                    MedicalServiceProviderProfileId = profile.Id,
                    DayOfWeek = item.DayOfWeek
                };
                _context.MedicalServiceProviderWorkingHours.Add(hour);
                profile.WorkingHours.Add(hour);
            }

            hour.IsClosed = item.IsClosed;
            hour.OpenTime = item.IsClosed ? null : ParseTime(item.OpenTime);
            hour.CloseTime = item.IsClosed ? null : ParseTime(item.CloseTime);
        }

        profile.UpdatedAt = DateTime.UtcNow;
        UnpublishIfIncomplete(profile);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var result = profile.WorkingHours
            .OrderBy(hour => DayOrder(hour.DayOfWeek))
            .Select(ToWorkingHourDto)
            .ToList();

        return Result<IReadOnlyList<MedicalServiceProviderWorkingHourDto>>.Success(
            result,
            "Working hours updated successfully.");
    }

    internal static List<string> MissingRequirements(
        MedicalServiceProviderProfile profile)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(profile.BusinessName)) missing.Add("Business name");
        if (!profile.ProviderType.HasValue) missing.Add("Provider type");
        if (string.IsNullOrWhiteSpace(profile.Description)) missing.Add("Description");
        if (string.IsNullOrWhiteSpace(profile.PhoneNumber)) missing.Add("Phone number");
        if (string.IsNullOrWhiteSpace(profile.Address)) missing.Add("Address");
        if (string.IsNullOrWhiteSpace(profile.Governorate)) missing.Add("Governorate");
        if (string.IsNullOrWhiteSpace(profile.City)) missing.Add("City");
        if (!profile.Latitude.HasValue || !profile.Longitude.HasValue)
        {
            missing.Add("Valid location coordinates");
        }

        if (!profile.ServiceOfferings.Any(service =>
                service.IsActive && service.MedicalServiceCategory?.IsActive == true))
        {
            missing.Add("At least one active service in an active category");
        }

        var validHours = profile.WorkingHours.Count == 7
                         && profile.WorkingHours.Select(hour => hour.DayOfWeek).Distinct().Count() == 7
                         && profile.WorkingHours.All(hour =>
                             hour.IsClosed ||
                             (hour.OpenTime.HasValue &&
                              hour.CloseTime.HasValue &&
                              hour.OpenTime < hour.CloseTime))
                         && profile.WorkingHours.Any(hour => !hour.IsClosed);
        if (!validHours)
        {
            missing.Add("A valid seven-day working-hours schedule with at least one open day");
        }

        return missing;
    }

    private async Task<MedicalServiceProviderProfile?> LoadProfileAsync(
        string userId,
        bool tracking,
        CancellationToken ct)
    {
        var query = _context.MedicalServiceProviderProfiles
            .Include(profile => profile.User)
            .Include(profile => profile.ServiceOfferings)
                .ThenInclude(service => service.MedicalServiceCategory)
            .Include(profile => profile.WorkingHours)
            .Where(profile => profile.UserId == userId && profile.User!.IsActive);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(ct);
    }

    private async Task<MedicalServiceOffering?> LoadOwnedServiceAsync(
        string userId,
        Guid serviceId,
        bool tracking,
        CancellationToken ct)
    {
        var query = _context.MedicalServiceOfferings
            .Include(service => service.MedicalServiceCategory)
            .Include(service => service.MedicalServiceProviderProfile)
                .ThenInclude(profile => profile!.User)
            .Where(service =>
                service.Id == serviceId &&
                service.MedicalServiceProviderProfile!.UserId == userId &&
                service.MedicalServiceProviderProfile.User!.IsActive);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(ct);
    }

    private async Task<Guid?> ResolveProfileIdAsync(string userId, CancellationToken ct) =>
        await _context.MedicalServiceProviderProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId && profile.User!.IsActive)
            .Select(profile => (Guid?)profile.Id)
            .FirstOrDefaultAsync(ct);

    private Task<bool> ServiceNameExistsAsync(
        Guid profileId,
        string name,
        Guid? excludingId,
        CancellationToken ct) =>
        _context.MedicalServiceOfferings.AnyAsync(
            service =>
                service.MedicalServiceProviderProfileId == profileId &&
                service.Name.ToLower() == name.ToLower() &&
                (!excludingId.HasValue || service.Id != excludingId.Value),
            ct);

    private async Task ReloadReadinessAsync(
        MedicalServiceProviderProfile profile,
        CancellationToken ct)
    {
        await _context.Entry(profile)
            .Collection(item => item.ServiceOfferings)
            .Query()
            .Include(service => service.MedicalServiceCategory)
            .LoadAsync(ct);
        await _context.Entry(profile).Collection(item => item.WorkingHours).LoadAsync(ct);
    }

    private static void UnpublishIfIncomplete(MedicalServiceProviderProfile profile)
    {
        if (profile.IsPublished && MissingRequirements(profile).Count > 0)
        {
            profile.IsPublished = false;
        }
    }

    private static MedicalServiceProviderProfileDto ToProfileDto(
        MedicalServiceProviderProfile profile)
    {
        var missing = MissingRequirements(profile);
        return new MedicalServiceProviderProfileDto
        {
            Id = profile.Id,
            BusinessName = profile.BusinessName,
            ProviderType = profile.ProviderType,
            ProviderTypeName = profile.ProviderType?.ToString(),
            Description = profile.Description,
            PhoneNumber = profile.PhoneNumber,
            Address = profile.Address,
            Governorate = profile.Governorate,
            City = profile.City,
            Latitude = profile.Latitude,
            Longitude = profile.Longitude,
            IsPublished = profile.IsPublished,
            IsReadyToPublish = missing.Count == 0,
            MissingRequirements = missing,
            ActiveServicesCount = profile.ServiceOfferings.Count(service => service.IsActive),
            InactiveServicesCount = profile.ServiceOfferings.Count(service => !service.IsActive),
            ServiceCategoriesCount = profile.ServiceOfferings
                .Select(service => service.MedicalServiceCategoryId)
                .Distinct()
                .Count(),
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }

    private static MedicalServiceOfferingDto ToServiceDto(
        MedicalServiceOffering service) => new()
    {
        Id = service.Id,
        CategoryId = service.MedicalServiceCategoryId,
        CategoryName = service.MedicalServiceCategory?.Name ?? string.Empty,
        Name = service.Name,
        Description = service.Description,
        Price = service.Price,
        EstimatedDurationMinutes = service.EstimatedDurationMinutes,
        PreparationInstructions = service.PreparationInstructions,
        DeliveryModeAvailability = service.DeliveryModeAvailability,
        IsActive = service.IsActive,
        CreatedAt = service.CreatedAt,
        UpdatedAt = service.UpdatedAt
    };

    private static MedicalServiceProviderWorkingHourDto ToWorkingHourDto(
        MedicalServiceProviderWorkingHour hour) => new()
    {
        Id = hour.Id,
        DayOfWeek = hour.DayOfWeek,
        DayName = hour.DayOfWeek.ToString(),
        OpenTime = hour.OpenTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
        CloseTime = hour.CloseTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
        IsClosed = hour.IsClosed
    };

    private static TimeOnly? ParseTime(string? value) =>
        TimeOnly.TryParseExact(
            value,
            ["HH:mm", "HH:mm:ss"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var time)
            ? time
            : null;

    private static int DayOrder(DayOfWeek day) =>
        day == DayOfWeek.Saturday ? 0 : (int)day + 1;

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
