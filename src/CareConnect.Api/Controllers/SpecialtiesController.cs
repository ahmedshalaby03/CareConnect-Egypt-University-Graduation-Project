using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Specialties;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The shared specialty lookup. Any signed-in account may read it, because doctors,
/// hospitals and patients all need the same list to fill in forms and filters.
/// </summary>
[Route("api/specialties")]
[Produces("application/json")]
[Authorize]
public class SpecialtiesController : ApiControllerBase
{
    private readonly ISpecialtyService _specialties;

    public SpecialtiesController(ISpecialtyService specialties) => _specialties = specialties;

    /// <summary>Active specialties only, sorted alphabetically by English name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SpecialtyOptionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var result = await _specialties.GetActiveAsync(ct);
        return FromResult(result);
    }
}
