using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.BloodStock;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The hospital's own blood stock. No profile id in any route: ownership always comes from
/// the authenticated account, so a hospital cannot reach another hospital's stock.
/// </summary>
[Route("api/hospital/blood-stock")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.HospitalOnly)]
public class HospitalBloodStockController : ApiControllerBase
{
    private readonly IBloodStockService _bloodStock;

    public HospitalBloodStockController(IBloodStockService bloodStock) => _bloodStock = bloodStock;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BloodStockDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] BloodStockQueryParameters query, CancellationToken ct)
    {
        var result = await _bloodStock.GetHospitalStockAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    [HttpGet("{bloodGroup}")]
    [ProducesResponseType(typeof(ApiResponse<BloodStockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByBloodGroup(BloodGroup bloodGroup, CancellationToken ct)
    {
        var result = await _bloodStock.GetHospitalStockByBloodGroupAsync(CurrentUserId, bloodGroup, ct);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BloodStockDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateBloodStockRequest request, CancellationToken ct)
    {
        var result = await _bloodStock.CreateAsync(CurrentUserId, request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BloodStockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateBloodStockRequest request, CancellationToken ct)
    {
        var result = await _bloodStock.UpdateAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/increase")]
    [ProducesResponseType(typeof(ApiResponse<BloodStockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Increase(Guid id, IncreaseBloodStockRequest request, CancellationToken ct)
    {
        var result = await _bloodStock.IncreaseAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    /// <summary>Rejects a decrease that would take AvailableUnits below zero.</summary>
    [HttpPatch("{id:guid}/decrease")]
    [ProducesResponseType(typeof(ApiResponse<BloodStockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Decrease(Guid id, DecreaseBloodStockRequest request, CancellationToken ct)
    {
        var result = await _bloodStock.DecreaseAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }
}
