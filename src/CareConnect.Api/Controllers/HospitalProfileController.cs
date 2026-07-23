using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Hospitals;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The hospital's own profile and specialty list.
///
/// As with the doctor controller there is no profile id in any route: ownership always
/// comes from the authenticated account.
/// </summary>
[Route("api/hospital/profile")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.HospitalOnly)]
public class HospitalProfileController : ApiControllerBase
{
    private readonly IHospitalProfileService _profiles;

    public HospitalProfileController(IHospitalProfileService profiles) => _profiles = profiles;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HospitalProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _profiles.GetOwnProfileAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    /// <summary>Replaces the hospital's details. IsProfileCompleted is recalculated server-side.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<HospitalProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(UpdateHospitalProfileRequest request, CancellationToken ct)
    {
        var result = await _profiles.UpdateOwnProfileAsync(CurrentUserId, request, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Replaces the hospital's specialty set with exactly the ids supplied. Only active
    /// specialties are accepted, and the swap runs in a transaction.
    /// </summary>
    [HttpPut("specialties")]
    [ProducesResponseType(typeof(ApiResponse<HospitalProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSpecialties(
        UpdateHospitalSpecialtiesRequest request,
        CancellationToken ct)
    {
        var result = await _profiles.UpdateOwnSpecialtiesAsync(CurrentUserId, request, ct);
        return FromResult(result);
    }

    [HttpGet("location")]
    [ProducesResponseType(typeof(ApiResponse<HospitalLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocation(CancellationToken ct)
    {
        var result = await _profiles.GetOwnLocationAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Updates only the hospital's location fields. IsProfileCompleted (the general profile
    /// flag) is recalculated since Address/Governorate/City live here too, but HospitalName,
    /// PhoneNumber and everything else on the profile are left untouched.
    /// </summary>
    [HttpPut("location")]
    [ProducesResponseType(typeof(ApiResponse<HospitalLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocation(UpdateHospitalLocationRequest request, CancellationToken ct)
    {
        var result = await _profiles.UpdateOwnLocationAsync(CurrentUserId, request, ct);
        return FromResult(result);
    }
}
