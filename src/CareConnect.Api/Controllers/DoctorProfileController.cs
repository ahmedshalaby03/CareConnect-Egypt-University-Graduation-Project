using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Doctors;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The doctor's own profile.
///
/// There is deliberately no profile id in any route here: the service resolves the profile
/// from the authenticated user, so a doctor cannot reach another doctor's record no matter
/// what they send.
/// </summary>
[Route("api/doctor/profile")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.DoctorOnly)]
public class DoctorProfileController : ApiControllerBase
{
    private readonly IDoctorProfileService _profiles;

    public DoctorProfileController(IDoctorProfileService profiles) => _profiles = profiles;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DoctorProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _profiles.GetOwnProfileAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Replaces the profile fields and, when supplied, updates the account's full name and
    /// phone number. IsProfileCompleted is recalculated server-side.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<DoctorProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(UpdateDoctorProfileRequest request, CancellationToken ct)
    {
        var result = await _profiles.UpdateOwnProfileAsync(CurrentUserId, request, ct);
        return FromResult(result);
    }
}
