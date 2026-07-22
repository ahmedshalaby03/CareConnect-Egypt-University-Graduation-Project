using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Affiliations;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The hospital's side of the doctor relationship: reviewing incoming requests and
/// managing the doctors currently working there.
///
/// The service checks that every request id belongs to the calling hospital, so a hospital
/// cannot approve or reject a request addressed to somebody else.
/// </summary>
[Route("api/hospital")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.HospitalOnly)]
public class HospitalDoctorsController : ApiControllerBase
{
    private readonly IDoctorHospitalAffiliationService _affiliations;

    public HospitalDoctorsController(IDoctorHospitalAffiliationService affiliations) =>
        _affiliations = affiliations;

    /// <summary>Incoming requests, filterable by status, doctor name and specialty.</summary>
    [HttpGet("doctor-requests")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<HospitalDoctorRequestDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRequests(
        [FromQuery] HospitalAffiliationQueryParameters query,
        CancellationToken ct)
    {
        var result = await _affiliations.GetHospitalRequestsAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    [HttpPatch("doctor-requests/{requestId:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<HospitalDoctorRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid requestId, CancellationToken ct)
    {
        var result = await _affiliations.ApproveRequestAsync(CurrentUserId, requestId, ct);
        return FromResult(result);
    }

    /// <summary>Declines a request. A reason is mandatory and is shown to the doctor.</summary>
    [HttpPatch("doctor-requests/{requestId:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<HospitalDoctorRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(
        Guid requestId,
        RejectAffiliationRequest request,
        CancellationToken ct)
    {
        var result = await _affiliations.RejectRequestAsync(CurrentUserId, requestId, request, ct);
        return FromResult(result);
    }

    /// <summary>Doctors currently approved at this hospital.</summary>
    [HttpGet("doctors")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<HospitalDoctorDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctors(
        [FromQuery] HospitalAffiliationQueryParameters query,
        CancellationToken ct)
    {
        var result = await _affiliations.GetHospitalDoctorsAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Ends an approved affiliation. The record moves to Removed and is kept, so the
    /// history of who worked where survives.
    /// </summary>
    [HttpPatch("doctors/{doctorProfileId:guid}/remove")]
    [ProducesResponseType(typeof(ApiResponse<HospitalDoctorRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDoctor(Guid doctorProfileId, CancellationToken ct)
    {
        var result = await _affiliations.RemoveDoctorAsync(CurrentUserId, doctorProfileId, ct);
        return FromResult(result);
    }
}
