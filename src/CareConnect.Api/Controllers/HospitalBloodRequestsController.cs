using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.BloodRequests;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The hospital's own blood requests. The service checks that every request id belongs to
/// the calling hospital, so a hospital cannot process another hospital's request.
/// </summary>
[Route("api/hospital/blood-requests")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.HospitalOnly)]
public class HospitalBloodRequestsController : ApiControllerBase
{
    private readonly IBloodRequestService _bloodRequests;

    public HospitalBloodRequestsController(IBloodRequestService bloodRequests) => _bloodRequests = bloodRequests;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<HospitalBloodRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] HospitalBloodRequestQueryParameters query, CancellationToken ct)
    {
        var result = await _bloodRequests.GetHospitalRequestsAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    /// <summary>Feeds the pending/emergency/awaiting-fulfillment widgets on the hospital dashboard.</summary>
    [HttpGet("dashboard-stats")]
    [ProducesResponseType(typeof(ApiResponse<HospitalBloodDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var result = await _bloodRequests.GetHospitalDashboardStatsAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<HospitalBloodRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _bloodRequests.GetHospitalRequestByIdAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    /// <summary>Allocates the requested units from stock and approves, in one transaction.</summary>
    [HttpPatch("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<HospitalBloodRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(Guid id, ApproveBloodRequestRequest request, CancellationToken ct)
    {
        var result = await _bloodRequests.ApproveAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    /// <summary>Declines a request. A reason is mandatory and is shown to the patient.</summary>
    [HttpPatch("{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<HospitalBloodRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid id, RejectBloodRequestRequest request, CancellationToken ct)
    {
        var result = await _bloodRequests.RejectAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    /// <summary>Marks an Approved request Fulfilled. Never decreases stock again.</summary>
    [HttpPatch("{id:guid}/fulfill")]
    [ProducesResponseType(typeof(ApiResponse<HospitalBloodRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Fulfill(Guid id, CancellationToken ct)
    {
        var result = await _bloodRequests.FulfillAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    /// <summary>Internal review notes. Never editable by the Patient; only while the decision is still open.</summary>
    [HttpPut("{id:guid}/notes")]
    [ProducesResponseType(typeof(ApiResponse<HospitalBloodRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNotes(Guid id, BloodRequestHospitalNotesRequest request, CancellationToken ct)
    {
        var result = await _bloodRequests.UpdateHospitalNotesAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }
}
