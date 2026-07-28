using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceProviders;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

public sealed class MedicalServiceCategoryService : IMedicalServiceCategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicalServiceCategoryService> _logger;

    public MedicalServiceCategoryService(
        ApplicationDbContext context,
        ILogger<MedicalServiceCategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<MedicalServiceCategoryOptionDto>>> GetActiveAsync(
        CancellationToken ct = default)
    {
        var items = await _context.MedicalServiceCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .Select(category => new MedicalServiceCategoryOptionDto
            {
                Id = category.Id,
                Name = category.Name
            })
            .ToListAsync(ct);

        return Result<IReadOnlyList<MedicalServiceCategoryOptionDto>>.Success(
            items,
            "Medical service categories retrieved successfully.");
    }

    public async Task<Result<PagedResult<MedicalServiceCategoryDto>>> GetAllAsync(
        MedicalServiceCategoryQueryParameters query,
        CancellationToken ct = default)
    {
        var categories = _context.MedicalServiceCategories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            categories = categories.Where(category =>
                EF.Functions.Like(category.Name, $"%{term}%") ||
                (category.Description != null &&
                 EF.Functions.Like(category.Description, $"%{term}%")));
        }

        if (query.IsActive.HasValue)
        {
            categories = categories.Where(category => category.IsActive == query.IsActive.Value);
        }

        var totalCount = await categories.CountAsync(ct);
        var items = await categories
            .OrderBy(category => category.Name)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(category => new MedicalServiceCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                ServiceUsageCount = category.ServiceOfferings.Count
            })
            .ToListAsync(ct);

        return Result<PagedResult<MedicalServiceCategoryDto>>.Success(
            PagedResult<MedicalServiceCategoryDto>.Create(
                items,
                query.Page,
                query.PageSize,
                totalCount),
            "Medical service categories retrieved successfully.");
    }

    public async Task<Result<MedicalServiceCategoryDto>> CreateAsync(
        CreateMedicalServiceCategoryRequest request,
        CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await NameExistsAsync(name, null, ct))
        {
            return Result<MedicalServiceCategoryDto>.Conflict(
                $"A medical service category named '{name}' already exists.");
        }

        var category = new MedicalServiceCategory
        {
            Name = name,
            Description = Normalise(request.Description),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.MedicalServiceCategories.Add(category);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created medical service category {CategoryId}.", category.Id);
        return Result<MedicalServiceCategoryDto>.Success(
            ToDto(category, 0),
            "Medical service category created successfully.");
    }

    public async Task<Result<MedicalServiceCategoryDto>> UpdateAsync(
        Guid id,
        UpdateMedicalServiceCategoryRequest request,
        CancellationToken ct = default)
    {
        var category = await _context.MedicalServiceCategories
            .FirstOrDefaultAsync(item => item.Id == id, ct);
        if (category is null)
        {
            return Result<MedicalServiceCategoryDto>.NotFound(
                "Medical service category not found.");
        }

        var name = request.Name.Trim();
        if (await NameExistsAsync(name, id, ct))
        {
            return Result<MedicalServiceCategoryDto>.Conflict(
                $"A medical service category named '{name}' already exists.");
        }

        category.Name = name;
        category.Description = Normalise(request.Description);
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        var usageCount = await _context.MedicalServiceOfferings
            .CountAsync(service => service.MedicalServiceCategoryId == id, ct);

        _logger.LogInformation("Updated medical service category {CategoryId}.", id);
        return Result<MedicalServiceCategoryDto>.Success(
            ToDto(category, usageCount),
            "Medical service category updated successfully.");
    }

    public async Task<Result<MedicalServiceCategoryDto>> SetStatusAsync(
        Guid id,
        SetMedicalServiceCategoryStatusRequest request,
        CancellationToken ct = default)
    {
        var category = await _context.MedicalServiceCategories
            .FirstOrDefaultAsync(item => item.Id == id, ct);
        if (category is null)
        {
            return Result<MedicalServiceCategoryDto>.NotFound(
                "Medical service category not found.");
        }

        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        if (!category.IsActive)
        {
            var now = DateTime.UtcNow;
            await _context.MedicalServiceProviderProfiles
                .Where(profile =>
                    profile.IsPublished &&
                    !profile.ServiceOfferings.Any(service =>
                        service.IsActive &&
                        service.MedicalServiceCategory!.IsActive))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(profile => profile.IsPublished, false)
                        .SetProperty(profile => profile.UpdatedAt, now),
                    ct);
        }

        var usageCount = await _context.MedicalServiceOfferings
            .CountAsync(service => service.MedicalServiceCategoryId == id, ct);

        _logger.LogInformation(
            "Set IsActive={IsActive} on medical service category {CategoryId}.",
            category.IsActive,
            category.Id);

        return Result<MedicalServiceCategoryDto>.Success(
            ToDto(category, usageCount),
            category.IsActive
                ? "Medical service category activated successfully."
                : "Medical service category deactivated successfully. Existing services were preserved.");
    }

    private Task<bool> NameExistsAsync(string name, Guid? excludingId, CancellationToken ct) =>
        _context.MedicalServiceCategories.AnyAsync(
            category =>
                category.Name.ToLower() == name.ToLower() &&
                (!excludingId.HasValue || category.Id != excludingId.Value),
            ct);

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static MedicalServiceCategoryDto ToDto(
        MedicalServiceCategory category,
        int usageCount) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        IsActive = category.IsActive,
        CreatedAt = category.CreatedAt,
        UpdatedAt = category.UpdatedAt,
        ServiceUsageCount = usageCount
    };
}
