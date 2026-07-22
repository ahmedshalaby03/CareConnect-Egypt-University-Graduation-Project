using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Scheduling;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>The doctor's own absences and vacations. Ownership always comes from the account.</summary>
[Route("api/doctor/unavailable-periods")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.DoctorOnly)]
public class DoctorUnavailablePeriodsController : ApiControllerBase
{
    private readonly IDoctorUnavailablePeriodService _periods;

    public DoctorUnavailablePeriodsController(IDoctorUnavailablePeriodService periods) => _periods = periods;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UnavailablePeriodDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] UnavailablePeriodQueryParameters query, CancellationToken ct)
    {
        var result = await _periods.GetOwnAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    /// <summary>Rejected with a clear message if the period overlaps a Pending or Confirmed appointment.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UnavailablePeriodDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateUnavailablePeriodRequest request, CancellationToken ct)
    {
        var result = await _periods.CreateAsync(CurrentUserId, request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Only a period that has not started yet can be removed.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _periods.DeleteAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }
}
