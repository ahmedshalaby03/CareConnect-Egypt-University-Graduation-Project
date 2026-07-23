using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.InsuranceRequests;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The hospital's own insurance requests. The service checks that every request id belongs
/// to the calling hospital, so a hospital cannot review another hospital's request.
/// </summary>
[Route("api/hospital/insurance-requests")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.HospitalOnly)]
public class HospitalInsuranceRequestsController : ApiControllerBase
{
    private readonly IInsuranceRequestService _insuranceRequests;

    public HospitalInsuranceRequestsController(IInsuranceRequestService insuranceRequests) =>
        _insuranceRequests = insuranceRequests;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<HospitalInsuranceRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] HospitalInsuranceRequestQueryParameters query,
        CancellationToken ct)
    {
        var result = await _insuranceRequests.GetHospitalRequestsAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    /// <summary>Feeds the pending/under-review/approved/rejected widgets on the hospital dashboard.</summary>
    [HttpGet("dashboard-stats")]
    [ProducesResponseType(typeof(ApiResponse<HospitalInsuranceDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var result = await _insuranceRequests.GetHospitalDashboardStatsAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<HospitalInsuranceRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _insuranceRequests.GetHospitalRequestByIdAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/start-review")]
    [ProducesResponseType(typeof(ApiResponse<HospitalInsuranceRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartReview(Guid id, CancellationToken ct)
    {
        var result = await _insuranceRequests.StartReviewAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<HospitalInsuranceRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid id, ApproveInsuranceRequestRequest request, CancellationToken ct)
    {
        var result = await _insuranceRequests.ApproveAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    /// <summary>Declines a request. A reason is mandatory and is shown to the patient.</summary>
    [HttpPatch("{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<HospitalInsuranceRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid id, RejectInsuranceRequestRequest request, CancellationToken ct)
    {
        var result = await _insuranceRequests.RejectAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    /// <summary>Internal review notes. Never editable by the Patient; only while the decision is still open.</summary>
    [HttpPut("{id:guid}/notes")]
    [ProducesResponseType(typeof(ApiResponse<HospitalInsuranceRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNotes(Guid id, InsuranceHospitalNotesRequest request, CancellationToken ct)
    {
        var result = await _insuranceRequests.UpdateHospitalNotesAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }
}
