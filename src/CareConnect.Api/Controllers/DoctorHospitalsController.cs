using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Affiliations;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The doctor's side of the hospital relationship: sending requests, tracking them, and
/// listing the hospitals they have been approved at.
/// </summary>
[Route("api/doctor")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.DoctorOnly)]
public class DoctorHospitalsController : ApiControllerBase
{
    private readonly IDoctorHospitalAffiliationService _affiliations;

    public DoctorHospitalsController(IDoctorHospitalAffiliationService affiliations) =>
        _affiliations = affiliations;

    /// <summary>The doctor's own requests, filterable by status and hospital name.</summary>
    [HttpGet("hospital-requests")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DoctorHospitalRequestDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRequests(
        [FromQuery] DoctorAffiliationQueryParameters query,
        CancellationToken ct)
    {
        var result = await _affiliations.GetDoctorRequestsAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Applies to work at a hospital. Requires a completed doctor profile, a completed
    /// hospital profile, and that the hospital lists the doctor's specialty.
    /// </summary>
    [HttpPost("hospital-requests")]
    [ProducesResponseType(typeof(ApiResponse<DoctorHospitalRequestDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRequest(CreateAffiliationRequest request, CancellationToken ct)
    {
        var result = await _affiliations.CreateRequestAsync(CurrentUserId, request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Withdraws a request that the hospital has not reviewed yet.</summary>
    [HttpPatch("hospital-requests/{requestId:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<DoctorHospitalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelRequest(Guid requestId, CancellationToken ct)
    {
        var result = await _affiliations.CancelRequestAsync(CurrentUserId, requestId, ct);
        return FromResult(result);
    }

    /// <summary>Marks one approved hospital as primary; any previous primary is cleared.</summary>
    [HttpPatch("hospitals/{hospitalId:guid}/set-primary")]
    [ProducesResponseType(typeof(ApiResponse<DoctorHospitalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimary(Guid hospitalId, CancellationToken ct)
    {
        var result = await _affiliations.SetPrimaryHospitalAsync(CurrentUserId, hospitalId, ct);
        return FromResult(result);
    }

    /// <summary>Hospitals the doctor currently works at, primary first.</summary>
    [HttpGet("hospitals")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DoctorAffiliatedHospitalDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHospitals(CancellationToken ct)
    {
        var result = await _affiliations.GetDoctorHospitalsAsync(CurrentUserId, ct);
        return FromResult(result);
    }
}
