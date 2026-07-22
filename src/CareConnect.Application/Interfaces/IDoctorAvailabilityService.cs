using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Scheduling;

namespace CareConnect.Application.Interfaces;

/// <summary>Scoped by the caller's user id - a doctor can only ever reach their own schedule.</summary>
public interface IDoctorAvailabilityService
{
    Task<Result<IReadOnlyList<AvailabilityDto>>> GetOwnAsync(
        string doctorUserId,
        AvailabilityQueryParameters query,
        CancellationToken ct = default);

    Task<Result<AvailabilityDto>> CreateAsync(
        string doctorUserId,
        CreateAvailabilityRequest request,
        CancellationToken ct = default);

    Task<Result<AvailabilityDto>> UpdateAsync(
        string doctorUserId,
        Guid id,
        UpdateAvailabilityRequest request,
        CancellationToken ct = default);

    /// <summary>Deactivating hides the block from slot generation; existing bookings are untouched.</summary>
    Task<Result<AvailabilityDto>> ToggleStatusAsync(
        string doctorUserId,
        Guid id,
        CancellationToken ct = default);
}
