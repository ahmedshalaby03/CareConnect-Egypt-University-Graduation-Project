using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Specialties;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// Specialty administration. Guarded at the controller level so any action added later is
/// protected by default rather than by remembering an attribute.
/// </summary>
[Route("api/super-admin/specialties")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
public class SuperAdminSpecialtiesController : ApiControllerBase
{
    private readonly ISpecialtyService _specialties;

    public SuperAdminSpecialtiesController(ISpecialtyService specialties) => _specialties = specialties;

    /// <summary>Lists specialties with search (English or Arabic), active filter and paging.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SpecialtyDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] SpecialtyQueryParameters query, CancellationToken ct)
    {
        var result = await _specialties.GetAllAsync(query, ct);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateSpecialtyRequest request, CancellationToken ct)
    {
        var result = await _specialties.CreateAsync(request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SpecialtyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateSpecialtyRequest request, CancellationToken ct)
    {
        var result = await _specialties.UpdateAsync(id, request, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Activates or deactivates a specialty. There is no delete: a deactivated specialty
    /// disappears from selection lists while every profile referencing it stays intact.
    /// </summary>
    [HttpPatch("{id:guid}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse<ToggleSpecialtyStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        var result = await _specialties.ToggleStatusAsync(id, ct);
        return FromResult(result);
    }
}
