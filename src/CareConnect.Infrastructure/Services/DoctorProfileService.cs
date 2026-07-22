using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Doctors;
using CareConnect.Application.DTOs.Specialties;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

public class DoctorProfileService : IDoctorProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DoctorProfileService> _logger;

    public DoctorProfileService(ApplicationDbContext context, ILogger<DoctorProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<DoctorProfileDto>> GetOwnProfileAsync(
        string userId,
        CancellationToken ct = default)
    {
        var profile = await LoadAsync(userId, tracking: false, ct);

        return profile is null
            ? Result<DoctorProfileDto>.NotFound("Doctor profile not found for the current account.")
            : Result<DoctorProfileDto>.Success(ToDto(profile), "Doctor profile retrieved successfully.");
    }

    public async Task<Result<DoctorProfileDto>> UpdateOwnProfileAsync(
        string userId,
        UpdateDoctorProfileRequest request,
        CancellationToken ct = default)
    {
        // Resolved from the authenticated user id, never from a client-supplied profile id,
        // so a doctor physically cannot address another doctor's row.
        var profile = await LoadAsync(userId, tracking: true, ct);
        if (profile is null)
        {
            return Result<DoctorProfileDto>.NotFound("Doctor profile not found for the current account.");
        }

        var user = profile.User!;

        if (request.SpecialtyId.HasValue)
        {
            var specialty = await _context.Specialties
                .FirstOrDefaultAsync(s => s.Id == request.SpecialtyId.Value, ct);

            if (specialty is null)
            {
                return Result<DoctorProfileDto>.Invalid(
                    "The selected specialty does not exist.",
                    ["SpecialtyId does not match any specialty."]);
            }

            // A doctor already assigned to a since-deactivated specialty may keep it, but
            // nobody may newly select one.
            if (!specialty.IsActive && profile.SpecialtyId != specialty.Id)
            {
                return Result<DoctorProfileDto>.Invalid(
                    $"The specialty '{specialty.Name}' is not currently available for selection.");
            }

            profile.Specialty = specialty;
        }
        else
        {
            // Clearing the specialty: drop the loaded navigation too, otherwise the
            // response would still echo the old one back.
            profile.Specialty = null;
        }

        profile.SpecialtyId = request.SpecialtyId;

        // Account fields follow "update when provided": omitting them leaves the account
        // untouched. Profile fields below are a full replace, matching PUT semantics.
        if (request.FullName is not null)
        {
            user.FullName = request.FullName.Trim();
        }

        if (request.PhoneNumber is not null)
        {
            var phone = Normalise(request.PhoneNumber);

            if (phone is not null && phone != user.PhoneNumber)
            {
                var taken = await _context.Users
                    .AnyAsync(u => u.PhoneNumber == phone && u.Id != user.Id, ct);

                if (taken)
                {
                    return Result<DoctorProfileDto>.Conflict(
                        "Another account is already using this phone number.");
                }
            }

            user.PhoneNumber = phone;
        }

        profile.LicenseNumber = Normalise(request.LicenseNumber);
        profile.YearsOfExperience = request.YearsOfExperience;
        profile.Biography = Normalise(request.Biography);
        profile.ConsultationPrice = request.ConsultationPrice;
        profile.Address = Normalise(request.Address);
        profile.Governorate = Normalise(request.Governorate);
        profile.City = Normalise(request.City);
        profile.ProfileImageUrl = Normalise(request.ProfileImageUrl);

        // Always derived here. The request has no field for it, so a client cannot claim
        // to be complete.
        profile.IsProfileCompleted = profile.HasRequiredProfileFields(user.FullName);
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Doctor {UserId} updated their profile (completed: {IsCompleted}).",
            userId, profile.IsProfileCompleted);

        return Result<DoctorProfileDto>.Success(ToDto(profile), "Doctor profile updated successfully.");
    }

    // ----------------------------------------------------------------- Helpers

    private async Task<DoctorProfile?> LoadAsync(string userId, bool tracking, CancellationToken ct)
    {
        var query = _context.DoctorProfiles
            .Include(p => p.User)
            .Include(p => p.Specialty)
            .Where(p => p.UserId == userId);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(ct);
    }

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Names the required fields that are still blank, for the UI's completion hint.</summary>
    internal static List<string> MissingFieldsFor(DoctorProfile profile, string? fullName)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(fullName)) missing.Add("Full name");
        if (!profile.SpecialtyId.HasValue) missing.Add("Specialty");
        if (string.IsNullOrWhiteSpace(profile.LicenseNumber)) missing.Add("License number");
        if (!profile.YearsOfExperience.HasValue) missing.Add("Years of experience");
        if (string.IsNullOrWhiteSpace(profile.Governorate)) missing.Add("Governorate");
        if (string.IsNullOrWhiteSpace(profile.City)) missing.Add("City");

        return missing;
    }

    internal static DoctorProfileDto ToDto(DoctorProfile profile)
    {
        var user = profile.User;

        return new DoctorProfileDto
        {
            Id = profile.Id,
            FullName = user?.FullName ?? string.Empty,
            Email = user?.Email ?? string.Empty,
            PhoneNumber = user?.PhoneNumber,
            Specialty = profile.Specialty is null
                ? null
                : new SpecialtyOptionDto
                {
                    Id = profile.Specialty.Id,
                    Name = profile.Specialty.Name,
                    ArabicName = profile.Specialty.ArabicName
                },
            LicenseNumber = profile.LicenseNumber,
            YearsOfExperience = profile.YearsOfExperience,
            Biography = profile.Biography,
            ConsultationPrice = profile.ConsultationPrice,
            Address = profile.Address,
            Governorate = profile.Governorate,
            City = profile.City,
            ProfileImageUrl = profile.ProfileImageUrl,
            IsProfileCompleted = profile.IsProfileCompleted,
            MissingFields = MissingFieldsFor(profile, user?.FullName),
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
