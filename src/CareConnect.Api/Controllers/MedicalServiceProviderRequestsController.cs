using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceRequests;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/medical-service-provider/requests")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.MedicalServiceProviderOnly)]
public sealed class MedicalServiceProviderRequestsController : ApiControllerBase
{
    private readonly IMedicalServiceRequestService _requests;

    public MedicalServiceProviderRequestsController(IMedicalServiceRequestService requests) =>
        _requests = requests;

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<MedicalServiceRequestSummaryDto>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] ProviderMedicalServiceRequestFilter filter,
        CancellationToken ct)
    {
        var result = await _requests.GetProviderRequestsAsync(CurrentUserId, filter, ct);
        return FromResult(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType(
        typeof(ApiResponse<MedicalServiceRequestDashboardSummaryDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var result = await _requests.GetProviderDashboardSummaryAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<MedicalServiceRequestDetailsDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _requests.GetProviderRequestByIdAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(
        Guid id,
        AcceptMedicalServiceRequestRequest request,
        CancellationToken ct)
    {
        var result = await _requests.AcceptAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id,
        RejectMedicalServiceRequestRequest request,
        CancellationToken ct)
    {
        var result = await _requests.RejectAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancelMedicalServiceRequestRequest request,
        CancellationToken ct)
    {
        var result = await _requests.CancelByProviderAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _requests.CompleteAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }
}
