using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.BloodRequests;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The patient's own blood requests. No profile id in any route: ownership always comes
/// from the authenticated account, so a patient cannot reach another patient's request.
/// </summary>
[Route("api/patient/blood-requests")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.PatientOnly)]
public class PatientBloodRequestsController : ApiControllerBase
{
    private readonly IBloodRequestService _bloodRequests;

    public PatientBloodRequestsController(IBloodRequestService bloodRequests) => _bloodRequests = bloodRequests;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientBloodRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] PatientBloodRequestQueryParameters query, CancellationToken ct)
    {
        var result = await _bloodRequests.GetPatientRequestsAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    /// <summary>Feeds the pending/approved widgets on the patient dashboard.</summary>
    [HttpGet("dashboard-stats")]
    [ProducesResponseType(typeof(ApiResponse<PatientBloodDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var result = await _bloodRequests.GetPatientDashboardStatsAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PatientBloodRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _bloodRequests.GetPatientRequestByIdAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Every eligibility and duplicate rule is revalidated against fresh data right before
    /// saving, so nothing here trusts what Angular displayed. Submission never touches stock.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PatientBloodRequestDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateBloodRequestRequest request, CancellationToken ct)
    {
        var result = await _bloodRequests.CreateRequestAsync(CurrentUserId, request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Withdraws the patient's own request while it is still Pending.</summary>
    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PatientBloodRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _bloodRequests.CancelRequestAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }
}
