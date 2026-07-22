using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Scheduling;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The doctor's own weekly schedule. No profile id appears in any route: ownership always
/// comes from the authenticated account, so a doctor cannot reach another doctor's schedule.
/// </summary>
[Route("api/doctor/availability")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.DoctorOnly)]
public class DoctorAvailabilityController : ApiControllerBase
{
    private readonly IDoctorAvailabilityService _availability;

    public DoctorAvailabilityController(IDoctorAvailabilityService availability) => _availability = availability;

    /// <summary>Filterable by hospital, day of week and active status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AvailabilityDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get([FromQuery] AvailabilityQueryParameters query, CancellationToken ct)
    {
        var result = await _availability.GetOwnAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    /// <summary>Requires an Approved affiliation with the selected hospital; rejects overlapping blocks.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AvailabilityDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateAvailabilityRequest request, CancellationToken ct)
    {
        var result = await _availability.CreateAsync(CurrentUserId, request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateAvailabilityRequest request, CancellationToken ct)
    {
        var result = await _availability.UpdateAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Activates or deactivates a block. Deactivating stops it generating new slots but
    /// never touches appointments already booked against it.
    /// </summary>
    [HttpPatch("{id:guid}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse<AvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        var result = await _availability.ToggleStatusAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }
}
