using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.BloodBank;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// Cross-hospital blood availability search. Open to any authenticated role - the Angular
/// client hides the "create request" action for non-Patient roles, but the read itself is
/// not Patient-only.
/// </summary>
[Route("api/blood-bank")]
[Produces("application/json")]
[Authorize]
public class BloodBankController : ApiControllerBase
{
    private readonly IBloodAvailabilityService _bloodAvailability;

    public BloodBankController(IBloodAvailabilityService bloodAvailability) =>
        _bloodAvailability = bloodAvailability;

    [HttpGet("availability")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BloodAvailabilityDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] BloodAvailabilityQueryParameters query,
        CancellationToken ct)
    {
        var result = await _bloodAvailability.SearchAsync(query, ct);
        return FromResult(result);
    }

    [HttpGet("hospitals/{hospitalProfileId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<HospitalBloodBankDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHospitalBloodBank(Guid hospitalProfileId, CancellationToken ct)
    {
        var result = await _bloodAvailability.GetHospitalBloodBankAsync(hospitalProfileId, ct);
        return FromResult(result);
    }
}
