using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Scheduling;

namespace CareConnect.Application.Interfaces;

public interface IDoctorUnavailablePeriodService
{
    Task<Result<IReadOnlyList<UnavailablePeriodDto>>> GetOwnAsync(
        string doctorUserId,
        UnavailablePeriodQueryParameters query,
        CancellationToken ct = default);

    /// <summary>Rejected if the range overlaps a Pending or Confirmed appointment.</summary>
    Task<Result<UnavailablePeriodDto>> CreateAsync(
        string doctorUserId,
        CreateUnavailablePeriodRequest request,
        CancellationToken ct = default);

    /// <summary>Only a period that has not started yet can be deleted.</summary>
    Task<Result<bool>> DeleteAsync(string doctorUserId, Guid id, CancellationToken ct = default);
}
