using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Specialties;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

public class SpecialtyService : ISpecialtyService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SpecialtyService> _logger;

    public SpecialtyService(ApplicationDbContext context, ILogger<SpecialtyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SpecialtyOptionDto>>> GetActiveAsync(CancellationToken ct = default)
    {
        var items = await _context.Specialties
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SpecialtyOptionDto
            {
                Id = s.Id,
                Name = s.Name,
                ArabicName = s.ArabicName
            })
            .ToListAsync(ct);

        return Result<IReadOnlyList<SpecialtyOptionDto>>.Success(
            items,
            "Specialties retrieved successfully.");
    }

    public async Task<Result<PagedResult<SpecialtyDto>>> GetAllAsync(
        SpecialtyQueryParameters query,
        CancellationToken ct = default)
    {
        var specialties = _context.Specialties.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Matched against both names so an Arabic search term works too.
            var term = query.Search.Trim();
            specialties = specialties.Where(s =>
                EF.Functions.Like(s.Name, $"%{term}%") ||
                (s.ArabicName != null && EF.Functions.Like(s.ArabicName, $"%{term}%")));
        }

        if (query.IsActive.HasValue)
        {
            specialties = specialties.Where(s => s.IsActive == query.IsActive.Value);
        }

        var totalCount = await specialties.CountAsync(ct);

        var items = await specialties
            .OrderBy(s => s.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(s => new SpecialtyDto
            {
                Id = s.Id,
                Name = s.Name,
                ArabicName = s.ArabicName,
                Description = s.Description,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                DoctorCount = s.DoctorProfiles.Count,
                HospitalCount = s.HospitalSpecialties.Count
            })
            .ToListAsync(ct);

        return Result<PagedResult<SpecialtyDto>>.Success(
            PagedResult<SpecialtyDto>.Create(items, query.Page, query.PageSize, totalCount),
            "Specialties retrieved successfully.");
    }

    public async Task<Result<SpecialtyDto>> CreateAsync(
        CreateSpecialtyRequest request,
        CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        var arabicName = Normalise(request.ArabicName);

        if (await NameExistsAsync(name, null, ct))
        {
            return Result<SpecialtyDto>.Conflict($"A specialty named '{name}' already exists.");
        }

        if (arabicName is not null && await ArabicNameExistsAsync(arabicName, null, ct))
        {
            return Result<SpecialtyDto>.Conflict(
                $"A specialty with the Arabic name '{arabicName}' already exists.");
        }

        var specialty = new Specialty
        {
            Name = name,
            ArabicName = arabicName,
            Description = Normalise(request.Description),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Specialties.Add(specialty);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created specialty {SpecialtyId} ({Name}).", specialty.Id, specialty.Name);

        return Result<SpecialtyDto>.Success(ToDto(specialty, 0, 0), "Specialty created successfully.");
    }

    public async Task<Result<SpecialtyDto>> UpdateAsync(
        Guid id,
        UpdateSpecialtyRequest request,
        CancellationToken ct = default)
    {
        var specialty = await _context.Specialties.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (specialty is null)
        {
            return Result<SpecialtyDto>.NotFound("Specialty not found.");
        }

        var name = request.Name.Trim();
        var arabicName = Normalise(request.ArabicName);

        // Excluding this row from the duplicate check, so saving without changing the name works.
        if (await NameExistsAsync(name, id, ct))
        {
            return Result<SpecialtyDto>.Conflict($"A specialty named '{name}' already exists.");
        }

        if (arabicName is not null && await ArabicNameExistsAsync(arabicName, id, ct))
        {
            return Result<SpecialtyDto>.Conflict(
                $"A specialty with the Arabic name '{arabicName}' already exists.");
        }

        specialty.Name = name;
        specialty.ArabicName = arabicName;
        specialty.Description = Normalise(request.Description);
        specialty.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        var doctorCount = await _context.DoctorProfiles.CountAsync(d => d.SpecialtyId == id, ct);
        var hospitalCount = await _context.HospitalSpecialties.CountAsync(h => h.SpecialtyId == id, ct);

        _logger.LogInformation("Updated specialty {SpecialtyId}.", id);

        return Result<SpecialtyDto>.Success(
            ToDto(specialty, doctorCount, hospitalCount),
            "Specialty updated successfully.");
    }

    public async Task<Result<ToggleSpecialtyStatusResponse>> ToggleStatusAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var specialty = await _context.Specialties.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (specialty is null)
        {
            return Result<ToggleSpecialtyStatusResponse>.NotFound("Specialty not found.");
        }

        // Deactivating is the only "delete". Existing doctor and hospital references stay
        // intact; the specialty simply stops appearing in selection lists.
        specialty.IsActive = !specialty.IsActive;
        specialty.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Set IsActive={IsActive} on specialty {SpecialtyId}.", specialty.IsActive, id);

        return Result<ToggleSpecialtyStatusResponse>.Success(
            new ToggleSpecialtyStatusResponse
            {
                Id = specialty.Id,
                Name = specialty.Name,
                IsActive = specialty.IsActive
            },
            specialty.IsActive
                ? "Specialty activated successfully."
                : "Specialty deactivated successfully. Existing profiles keep their assignment.");
    }

    // ----------------------------------------------------------------- Helpers

    private Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken ct) =>
        _context.Specialties.AnyAsync(
            s => s.Name.ToLower() == name.ToLower() && (excludingId == null || s.Id != excludingId),
            ct);

    private Task<bool> ArabicNameExistsAsync(string arabicName, Guid? excludingId, CancellationToken ct) =>
        _context.Specialties.AnyAsync(
            s => s.ArabicName == arabicName && (excludingId == null || s.Id != excludingId),
            ct);

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SpecialtyDto ToDto(Specialty specialty, int doctorCount, int hospitalCount) => new()
    {
        Id = specialty.Id,
        Name = specialty.Name,
        ArabicName = specialty.ArabicName,
        Description = specialty.Description,
        IsActive = specialty.IsActive,
        CreatedAt = specialty.CreatedAt,
        UpdatedAt = specialty.UpdatedAt,
        DoctorCount = doctorCount,
        HospitalCount = hospitalCount
    };
}
