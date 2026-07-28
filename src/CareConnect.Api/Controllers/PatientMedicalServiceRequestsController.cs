using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceRequests;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/patient/medical-service-requests")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.PatientOnly)]
public sealed class PatientMedicalServiceRequestsController : ApiControllerBase
{
    private readonly IMedicalServiceRequestService _requests;

    public PatientMedicalServiceRequestsController(IMedicalServiceRequestService requests) =>
        _requests = requests;

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<MedicalServiceRequestDetailsDto>),
        StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        CreateMedicalServiceRequestRequest request,
        CancellationToken ct)
    {
        var result = await _requests.CreateAsync(CurrentUserId, request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<MedicalServiceRequestSummaryDto>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] PatientMedicalServiceRequestFilter filter,
        CancellationToken ct)
    {
        var result = await _requests.GetPatientRequestsAsync(CurrentUserId, filter, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<MedicalServiceRequestDetailsDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _requests.GetPatientRequestByIdAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(
        typeof(ApiResponse<MedicalServiceRequestDetailsDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancelMedicalServiceRequestRequest request,
        CancellationToken ct)
    {
        var result = await _requests.CancelByPatientAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }
}
