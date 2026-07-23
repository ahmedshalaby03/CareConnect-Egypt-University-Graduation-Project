using System.Globalization;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Hospitals;
using CareConnect.Application.DTOs.Specialties;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

public class HospitalProfileService : IHospitalProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HospitalProfileService> _logger;

    public HospitalProfileService(ApplicationDbContext context, ILogger<HospitalProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<HospitalProfileDto>> GetOwnProfileAsync(
        string userId,
        CancellationToken ct = default)
    {
        var profile = await LoadAsync(userId, tracking: false, ct);

        return profile is null
            ? Result<HospitalProfileDto>.NotFound("Hospital profile not found for the current account.")
            : Result<HospitalProfileDto>.Success(ToDto(profile), "Hospital profile retrieved successfully.");
    }

    public async Task<Result<HospitalProfileDto>> UpdateOwnProfileAsync(
        string userId,
        UpdateHospitalProfileRequest request,
        CancellationToken ct = default)
    {
        // Ownership comes from the token, not from anything in the request body.
        var profile = await LoadAsync(userId, tracking: true, ct);
        if (profile is null)
        {
            return Result<HospitalProfileDto>.NotFound("Hospital profile not found for the current account.");
        }

        if (request.FullName is not null)
        {
            profile.User!.FullName = request.FullName.Trim();
        }

        profile.HospitalName = Normalise(request.HospitalName);
        profile.Address = Normalise(request.Address);
        profile.Governorate = Normalise(request.Governorate);
        profile.City = Normalise(request.City);
        profile.Latitude = request.Latitude;
        profile.Longitude = request.Longitude;
        profile.PhoneNumber = Normalise(request.PhoneNumber);
        profile.Description = Normalise(request.Description);
        profile.LogoUrl = Normalise(request.LogoUrl);
        profile.WebsiteUrl = Normalise(request.WebsiteUrl);
        profile.OpeningTime = ParseTime(request.OpeningTime);
        profile.ClosingTime = ParseTime(request.ClosingTime);

        // Derived server-side; the request has no field the client could use to fake it.
        profile.IsProfileCompleted = profile.HasRequiredProfileFields();
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Hospital {UserId} updated its profile (completed: {IsCompleted}).",
            userId, profile.IsProfileCompleted);

        return Result<HospitalProfileDto>.Success(ToDto(profile), "Hospital profile updated successfully.");
    }

    public async Task<Result<HospitalProfileDto>> UpdateOwnSpecialtiesAsync(
        string userId,
        UpdateHospitalSpecialtiesRequest request,
        CancellationToken ct = default)
    {
        var profile = await LoadAsync(userId, tracking: true, ct);
        if (profile is null)
        {
            return Result<HospitalProfileDto>.NotFound("Hospital profile not found for the current account.");
        }

        var requestedIds = request.SpecialtyIds.Distinct().ToList();

        var matching = await _context.Specialties
            .Where(s => requestedIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, s.IsActive })
            .ToListAsync(ct);

        var unknownIds = requestedIds.Except(matching.Select(s => s.Id)).ToList();
        if (unknownIds.Count > 0)
        {
            return Result<HospitalProfileDto>.Invalid(
                "One or more of the selected specialties do not exist.",
                unknownIds.Select(id => $"Unknown specialty id: {id}.").ToList());
        }

        // Only active specialties may be chosen. Anything the admin has since retired must
        // be dropped rather than re-selected.
        var inactive = matching.Where(s => !s.IsActive).ToList();
        if (inactive.Count > 0)
        {
            return Result<HospitalProfileDto>.Invalid(
                "One or more of the selected specialties are not currently available.",
                inactive.Select(s => $"'{s.Name}' is inactive and cannot be selected.").ToList());
        }

        var existing = await _context.HospitalSpecialties
            .Where(hs => hs.HospitalProfileId == profile.Id)
            .ToListAsync(ct);

        var toRemove = existing.Where(hs => !requestedIds.Contains(hs.SpecialtyId)).ToList();
        var existingIds = existing.Select(hs => hs.SpecialtyId).ToHashSet();
        var toAdd = requestedIds.Where(id => !existingIds.Contains(id)).ToList();

        // The delete and the insert have to land together: a partial apply would leave the
        // hospital advertising a specialty set it never chose.
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        if (toRemove.Count > 0)
        {
            // Removes only the join rows. The Specialty entities themselves are untouched,
            // which the Restrict delete behaviour also enforces at the database level.
            _context.HospitalSpecialties.RemoveRange(toRemove);
        }

        foreach (var specialtyId in toAdd)
        {
            _context.HospitalSpecialties.Add(new HospitalSpecialty
            {
                HospitalProfileId = profile.Id,
                SpecialtyId = specialtyId,
                CreatedAt = DateTime.UtcNow
            });
        }

        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation(
            "Hospital {UserId} set {Count} specialties (+{Added}, -{Removed}).",
            userId, requestedIds.Count, toAdd.Count, toRemove.Count);

        // Reloaded so the response carries the new specialty set rather than the stale one.
        var updated = await LoadAsync(userId, tracking: false, ct);

        return Result<HospitalProfileDto>.Success(
            ToDto(updated!),
            "Hospital specialties updated successfully.");
    }

    public async Task<Result<HospitalLocationDto>> GetOwnLocationAsync(
        string userId,
        CancellationToken ct = default)
    {
        var profile = await LoadAsync(userId, tracking: false, ct);

        return profile is null
            ? Result<HospitalLocationDto>.NotFound("Hospital profile not found for the current account.")
            : Result<HospitalLocationDto>.Success(ToLocationDto(profile), "Hospital location retrieved successfully.");
    }

    public async Task<Result<HospitalLocationDto>> UpdateOwnLocationAsync(
        string userId,
        UpdateHospitalLocationRequest request,
        CancellationToken ct = default)
    {
        // Ownership comes from the token, not from anything in the request body - a hospital
        // can never reach another hospital's location this way.
        var profile = await LoadAsync(userId, tracking: true, ct);
        if (profile is null)
        {
            return Result<HospitalLocationDto>.NotFound("Hospital profile not found for the current account.");
        }

        // Only the location fields are touched - HospitalName, PhoneNumber, specialties etc.
        // stay exactly as they were.
        profile.Address = Normalise(request.Address);
        profile.Governorate = Normalise(request.Governorate);
        profile.City = Normalise(request.City);
        profile.Latitude = request.Latitude;
        profile.Longitude = request.Longitude;
        profile.LocationDescription = Normalise(request.LocationDescription);
        profile.NearbyLandmark = Normalise(request.NearbyLandmark);

        profile.IsProfileCompleted = profile.HasRequiredProfileFields();
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Hospital {UserId} updated its location (completed: {IsLocationCompleted}).",
            userId, profile.HasCompletedLocation());

        return Result<HospitalLocationDto>.Success(ToLocationDto(profile), "Hospital location updated successfully.");
    }

    // ----------------------------------------------------------------- Helpers

    private async Task<HospitalProfile?> LoadAsync(string userId, bool tracking, CancellationToken ct)
    {
        var query = _context.HospitalProfiles
            .Include(p => p.User)
            .Include(p => p.HospitalSpecialties)
                .ThenInclude(hs => hs.Specialty)
            .Where(p => p.UserId == userId);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(ct);
    }

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TimeOnly? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // The validator already accepted one of these two shapes.
        if (TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hm))
        {
            return hm;
        }

        return TimeOnly.TryParseExact(value, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hms)
            ? hms
            : null;
    }

    internal static List<string> MissingFieldsFor(HospitalProfile profile)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(profile.HospitalName)) missing.Add("Hospital name");
        if (string.IsNullOrWhiteSpace(profile.Address)) missing.Add("Address");
        if (string.IsNullOrWhiteSpace(profile.Governorate)) missing.Add("Governorate");
        if (string.IsNullOrWhiteSpace(profile.City)) missing.Add("City");
        if (string.IsNullOrWhiteSpace(profile.PhoneNumber)) missing.Add("Phone number");

        return missing;
    }

    private static HospitalLocationDto ToLocationDto(HospitalProfile profile) => new()
    {
        HospitalProfileId = profile.Id,
        Address = profile.Address,
        Governorate = profile.Governorate,
        City = profile.City,
        Latitude = profile.Latitude,
        Longitude = profile.Longitude,
        LocationDescription = profile.LocationDescription,
        NearbyLandmark = profile.NearbyLandmark,
        IsLocationCompleted = profile.HasCompletedLocation(),
        UpdatedAt = profile.UpdatedAt
    };

    internal static HospitalProfileDto ToDto(HospitalProfile profile) => new()
    {
        Id = profile.Id,
        FullName = profile.User?.FullName ?? string.Empty,
        Email = profile.User?.Email ?? string.Empty,
        AccountPhoneNumber = profile.User?.PhoneNumber,
        HospitalName = profile.HospitalName,
        Address = profile.Address,
        Governorate = profile.Governorate,
        City = profile.City,
        Latitude = profile.Latitude,
        Longitude = profile.Longitude,
        PhoneNumber = profile.PhoneNumber,
        Description = profile.Description,
        LogoUrl = profile.LogoUrl,
        WebsiteUrl = profile.WebsiteUrl,
        OpeningTime = profile.OpeningTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
        ClosingTime = profile.ClosingTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
        IsProfileCompleted = profile.IsProfileCompleted,
        MissingFields = MissingFieldsFor(profile),
        Specialties = profile.HospitalSpecialties
            .Where(hs => hs.Specialty is not null)
            .Select(hs => new SpecialtyOptionDto
            {
                Id = hs.Specialty!.Id,
                Name = hs.Specialty.Name,
                ArabicName = hs.Specialty.ArabicName
            })
            .OrderBy(s => s.Name)
            .ToList(),
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt
    };
}
