using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.BloodBank;

namespace CareConnect.Application.Interfaces;

/// <summary>
/// Read-only, cross-hospital blood availability search. Any authenticated user may call
/// this - it never exposes hospital-internal detail (Notes, LastUpdatedByUserId).
/// </summary>
public interface IBloodAvailabilityService
{
    Task<Result<PagedResult<BloodAvailabilityDto>>> SearchAsync(
        BloodAvailabilityQueryParameters query,
        CancellationToken ct = default);

    Task<Result<HospitalBloodBankDetailsDto>> GetHospitalBloodBankAsync(
        Guid hospitalProfileId,
        CancellationToken ct = default);
}
