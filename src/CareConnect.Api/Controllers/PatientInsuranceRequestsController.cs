using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.InsuranceRequests;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The patient's own insurance requests. No profile id in any route: ownership always
/// comes from the authenticated account, so a patient cannot reach another patient's request.
/// </summary>
[Route("api/patient/insurance-requests")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.PatientOnly)]
public class PatientInsuranceRequestsController : ApiControllerBase
{
    private readonly IInsuranceRequestService _insuranceRequests;

    public PatientInsuranceRequestsController(IInsuranceRequestService insuranceRequests) =>
        _insuranceRequests = insuranceRequests;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientInsuranceRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] PatientInsuranceRequestQueryParameters query,
        CancellationToken ct)
    {
        var result = await _insuranceRequests.GetPatientRequestsAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    /// <summary>Feeds the pending/approved widgets on the patient dashboard.</summary>
    [HttpGet("dashboard-stats")]
    [ProducesResponseType(typeof(ApiResponse<PatientInsuranceDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var result = await _insuranceRequests.GetPatientDashboardStatsAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PatientInsuranceRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _insuranceRequests.GetPatientRequestByIdAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Every eligibility and duplicate rule is revalidated against fresh data inside a
    /// transaction right before saving, so nothing here trusts what Angular displayed.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PatientInsuranceRequestDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateInsuranceRequestRequest request, CancellationToken ct)
    {
        var result = await _insuranceRequests.CreateRequestAsync(CurrentUserId, request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Withdraws the patient's own request while it is still Pending.</summary>
    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PatientInsuranceRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _insuranceRequests.CancelRequestAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }
}
