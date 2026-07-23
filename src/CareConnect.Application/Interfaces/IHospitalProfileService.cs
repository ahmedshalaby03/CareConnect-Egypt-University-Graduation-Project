using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Hospitals;

namespace CareConnect.Application.Interfaces;

/// <summary>
/// Scoped by the signed-in user's id, so a hospital account can only reach its own profile
/// and its own specialty list.
/// </summary>
public interface IHospitalProfileService
{
    Task<Result<HospitalProfileDto>> GetOwnProfileAsync(string userId, CancellationToken ct = default);

    Task<Result<HospitalProfileDto>> UpdateOwnProfileAsync(
        string userId,
        UpdateHospitalProfileRequest request,
        CancellationToken ct = default);

    /// <summary>Replaces the hospital's specialty set with exactly the ids supplied.</summary>
    Task<Result<HospitalProfileDto>> UpdateOwnSpecialtiesAsync(
        string userId,
        UpdateHospitalSpecialtiesRequest request,
        CancellationToken ct = default);

    Task<Result<HospitalLocationDto>> GetOwnLocationAsync(string userId, CancellationToken ct = default);

    /// <summary>Touches only the location fields - HospitalName, PhoneNumber etc. are left untouched.</summary>
    Task<Result<HospitalLocationDto>> UpdateOwnLocationAsync(
        string userId,
        UpdateHospitalLocationRequest request,
        CancellationToken ct = default);
}
