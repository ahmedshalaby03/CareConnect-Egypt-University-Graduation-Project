using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Doctors;

namespace CareConnect.Application.Interfaces;

/// <summary>
/// Every method is scoped by the signed-in user's id rather than a profile id from the
/// route, so a doctor can only ever reach their own profile.
/// </summary>
public interface IDoctorProfileService
{
    Task<Result<DoctorProfileDto>> GetOwnProfileAsync(string userId, CancellationToken ct = default);

    Task<Result<DoctorProfileDto>> UpdateOwnProfileAsync(
        string userId,
        UpdateDoctorProfileRequest request,
        CancellationToken ct = default);
}
