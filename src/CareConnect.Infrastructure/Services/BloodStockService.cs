using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.BloodRequests;
using CareConnect.Application.DTOs.BloodStock;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Entities;
using CareConnect.Domain.Enums;
using CareConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareConnect.Infrastructure.Services;

/// <summary>
/// A hospital's own blood stock. Ownership always comes from the caller's user id, never
/// from a route id - see <see cref="LoadOwnedStockAsync"/>.
/// </summary>
public class BloodStockService : IBloodStockService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BloodStockService> _logger;

    public BloodStockService(ApplicationDbContext context, ILogger<BloodStockService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<BloodStockDto>>> GetHospitalStockAsync(
        string hospitalUserId,
        BloodStockQueryParameters query,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<IReadOnlyList<BloodStockDto>>.NotFound(
                "Hospital profile not found for the current account.");
        }

        var stock = _context.BloodStocks
            .AsNoTracking()
            .Where(s => s.HospitalProfileId == hospitalProfileId.Value);

        if (query.BloodGroup.HasValue)
        {
            stock = stock.Where(s => s.BloodGroup == query.BloodGroup.Value);
        }

        if (query.IsAvailable.HasValue)
        {
            stock = stock.Where(s => s.IsAvailable == query.IsAvailable.Value);
        }

        if (query.IsBelowMinimum.HasValue)
        {
            stock = query.IsBelowMinimum.Value
                ? stock.Where(s => s.AvailableUnits < s.MinimumRequiredUnits)
                : stock.Where(s => s.AvailableUnits >= s.MinimumRequiredUnits);
        }

        var items = await stock
            .OrderBy(s => s.BloodGroup)
            .Select(Projection())
            .ToListAsync(ct);

        return Result<IReadOnlyList<BloodStockDto>>.Success(items, "Blood stock retrieved successfully.");
    }

    public async Task<Result<BloodStockDto>> GetHospitalStockByBloodGroupAsync(
        string hospitalUserId,
        BloodGroup bloodGroup,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<BloodStockDto>.NotFound("Hospital profile not found for the current account.");
        }

        var dto = await _context.BloodStocks
            .AsNoTracking()
            .Where(s => s.HospitalProfileId == hospitalProfileId.Value && s.BloodGroup == bloodGroup)
            .Select(Projection())
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result<BloodStockDto>.NotFound($"No stock record exists for {bloodGroup.ToDisplayName()}.")
            : Result<BloodStockDto>.Success(dto, "Blood stock retrieved successfully.");
    }

    public async Task<Result<BloodStockDto>> CreateAsync(
        string hospitalUserId,
        CreateBloodStockRequest request,
        CancellationToken ct = default)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return Result<BloodStockDto>.NotFound("Hospital profile not found for the current account.");
        }

        var exists = await _context.BloodStocks.AnyAsync(
            s => s.HospitalProfileId == hospitalProfileId.Value && s.BloodGroup == request.BloodGroup, ct);

        if (exists)
        {
            return Result<BloodStockDto>.Conflict(
                $"A stock record for {request.BloodGroup.ToDisplayName()} already exists. Use update instead.");
        }

        var stock = new Domain.Entities.BloodStock
        {
            HospitalProfileId = hospitalProfileId.Value,
            BloodGroup = request.BloodGroup,
            AvailableUnits = request.AvailableUnits,
            MinimumRequiredUnits = request.MinimumRequiredUnits,
            Notes = Normalise(request.Notes),
            IsAvailable = request.AvailableUnits > 0,
            LastUpdatedByUserId = hospitalUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.BloodStocks.Add(stock);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created blood stock {StockId} ({BloodGroup}) for hospital {HospitalProfileId}.",
            stock.Id, stock.BloodGroup, hospitalProfileId);

        return Result<BloodStockDto>.Success(await ReloadDtoAsync(stock.Id, ct), "Blood stock created successfully.");
    }

    public async Task<Result<BloodStockDto>> UpdateAsync(
        string hospitalUserId,
        Guid id,
        UpdateBloodStockRequest request,
        CancellationToken ct = default)
    {
        var (failure, stock) = await LoadOwnedStockAsync(hospitalUserId, id, ct);
        if (failure is not null)
        {
            return failure;
        }

        stock!.AvailableUnits = request.AvailableUnits;
        stock.MinimumRequiredUnits = request.MinimumRequiredUnits;
        stock.Notes = Normalise(request.Notes);
        stock.IsAvailable = request.AvailableUnits > 0;
        stock.LastUpdatedByUserId = hospitalUserId;
        stock.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return Result<BloodStockDto>.Success(await ReloadDtoAsync(id, ct), "Blood stock updated successfully.");
    }

    public async Task<Result<BloodStockDto>> IncreaseAsync(
        string hospitalUserId,
        Guid id,
        IncreaseBloodStockRequest request,
        CancellationToken ct = default)
    {
        var (failure, stock) = await LoadOwnedStockAsync(hospitalUserId, id, ct);
        if (failure is not null)
        {
            return failure;
        }

        stock!.AvailableUnits += request.Units;
        stock.IsAvailable = stock.AvailableUnits > 0;
        stock.LastUpdatedByUserId = hospitalUserId;
        stock.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            stock.Notes = request.Notes.Trim();
        }

        await _context.SaveChangesAsync(ct);

        return Result<BloodStockDto>.Success(await ReloadDtoAsync(id, ct), "Blood stock increased successfully.");
    }

    public async Task<Result<BloodStockDto>> DecreaseAsync(
        string hospitalUserId,
        Guid id,
        DecreaseBloodStockRequest request,
        CancellationToken ct = default)
    {
        var (failure, stock) = await LoadOwnedStockAsync(hospitalUserId, id, ct);
        if (failure is not null)
        {
            return failure;
        }

        // Never trust Angular's arithmetic - the current value is re-read from the tracked
        // entity above, and the whole request is a single round trip to SaveChanges.
        if (request.Units > stock!.AvailableUnits)
        {
            return Result<BloodStockDto>.Invalid(
                $"Cannot remove {request.Units} units - only {stock.AvailableUnits} are available.");
        }

        stock.AvailableUnits -= request.Units;
        stock.IsAvailable = stock.AvailableUnits > 0;
        stock.LastUpdatedByUserId = hospitalUserId;
        stock.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            stock.Notes = request.Notes.Trim();
        }

        await _context.SaveChangesAsync(ct);

        return Result<BloodStockDto>.Success(await ReloadDtoAsync(id, ct), "Blood stock decreased successfully.");
    }

    public async Task<Result<SuperAdminBloodDashboardStatsDto>> GetSuperAdminDashboardStatsAsync(
        CancellationToken ct = default)
    {
        var hospitalsWithStock = await _context.BloodStocks
            .AsNoTracking()
            .Select(s => s.HospitalProfileId)
            .Distinct()
            .CountAsync(ct);

        var activeStockRecords = await _context.BloodStocks
            .AsNoTracking()
            .CountAsync(s => s.IsAvailable, ct);

        return Result<SuperAdminBloodDashboardStatsDto>.Success(
            new SuperAdminBloodDashboardStatsDto
            {
                HospitalsWithStockCount = hospitalsWithStock,
                ActiveBloodStockRecordsCount = activeStockRecords
            },
            "Dashboard statistics retrieved successfully.");
    }

    // ----------------------------------------------------------------- Helpers

    private Task<Guid?> GetHospitalProfileIdAsync(string userId, CancellationToken ct) =>
        _context.HospitalProfiles
            .Where(h => h.UserId == userId)
            .Select(h => (Guid?)h.Id)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Loads a stock row and proves it belongs to the calling hospital. Returns the same
    /// "not found" for a missing row and for another hospital's row, so the endpoint cannot
    /// be used to probe for valid stock ids.
    /// </summary>
    private async Task<(Result<BloodStockDto>? Failure, Domain.Entities.BloodStock? Stock)> LoadOwnedStockAsync(
        string hospitalUserId, Guid id, CancellationToken ct)
    {
        var hospitalProfileId = await GetHospitalProfileIdAsync(hospitalUserId, ct);
        if (hospitalProfileId is null)
        {
            return (Result<BloodStockDto>.NotFound("Hospital profile not found for the current account."), null);
        }

        var stock = await _context.BloodStocks.FirstOrDefaultAsync(s => s.Id == id, ct);

        if (stock is null || stock.HospitalProfileId != hospitalProfileId.Value)
        {
            return (Result<BloodStockDto>.NotFound("Blood stock record not found."), null);
        }

        return (null, stock);
    }

    private async Task<BloodStockDto> ReloadDtoAsync(Guid id, CancellationToken ct) =>
        await _context.BloodStocks
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(Projection())
            .FirstAsync(ct);

    private static string? Normalise(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static System.Linq.Expressions.Expression<Func<Domain.Entities.BloodStock, BloodStockDto>> Projection() =>
        s => new BloodStockDto
        {
            Id = s.Id,
            HospitalProfileId = s.HospitalProfileId,
            BloodGroup = s.BloodGroup,
            BloodGroupDisplayName = s.BloodGroup.ToDisplayName(),
            AvailableUnits = s.AvailableUnits,
            MinimumRequiredUnits = s.MinimumRequiredUnits,
            Notes = s.Notes,
            IsAvailable = s.IsAvailable,
            IsBelowMinimum = s.AvailableUnits < s.MinimumRequiredUnits,
            LastUpdatedByName = s.LastUpdatedByUser != null ? s.LastUpdatedByUser.FullName : null,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
}
