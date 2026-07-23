using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.BloodRequests;
using CareConnect.Application.DTOs.BloodStock;
using CareConnect.Domain.Enums;

namespace CareConnect.Application.Interfaces;

/// <summary>
/// A hospital's own blood stock. Every method resolves the caller's HospitalProfile from
/// the authenticated user id, never from a route or body id, so a hospital can only ever
/// reach its own stock.
/// </summary>
public interface IBloodStockService
{
    Task<Result<IReadOnlyList<BloodStockDto>>> GetHospitalStockAsync(
        string hospitalUserId,
        BloodStockQueryParameters query,
        CancellationToken ct = default);

    Task<Result<BloodStockDto>> GetHospitalStockByBloodGroupAsync(
        string hospitalUserId,
        BloodGroup bloodGroup,
        CancellationToken ct = default);

    Task<Result<BloodStockDto>> CreateAsync(
        string hospitalUserId,
        CreateBloodStockRequest request,
        CancellationToken ct = default);

    Task<Result<BloodStockDto>> UpdateAsync(
        string hospitalUserId,
        Guid id,
        UpdateBloodStockRequest request,
        CancellationToken ct = default);

    Task<Result<BloodStockDto>> IncreaseAsync(
        string hospitalUserId,
        Guid id,
        IncreaseBloodStockRequest request,
        CancellationToken ct = default);

    /// <summary>Rejects a decrease that would take AvailableUnits below zero.</summary>
    Task<Result<BloodStockDto>> DecreaseAsync(
        string hospitalUserId,
        Guid id,
        DecreaseBloodStockRequest request,
        CancellationToken ct = default);

    /// <summary>Backs the SuperAdmin dashboard - counts only, no per-hospital detail.</summary>
    Task<Result<SuperAdminBloodDashboardStatsDto>> GetSuperAdminDashboardStatsAsync(
        CancellationToken ct = default);
}
