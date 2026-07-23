using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.InsuranceCompanies;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// Insurance company administration. Guarded at the controller level so any action added
/// later is protected by default rather than by remembering an attribute.
/// </summary>
[Route("api/super-admin/insurance-companies")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
public class SuperAdminInsuranceCompaniesController : ApiControllerBase
{
    private readonly IInsuranceCompanyService _insuranceCompanies;

    public SuperAdminInsuranceCompaniesController(IInsuranceCompanyService insuranceCompanies) =>
        _insuranceCompanies = insuranceCompanies;

    /// <summary>Lists insurance companies with search (English or Arabic), active filter and paging.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InsuranceCompanyDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] InsuranceCompanyQueryParameters query, CancellationToken ct)
    {
        var result = await _insuranceCompanies.GetAllAsync(query, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InsuranceCompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _insuranceCompanies.GetByIdAsync(id, ct);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InsuranceCompanyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateInsuranceCompanyRequest request, CancellationToken ct)
    {
        var result = await _insuranceCompanies.CreateAsync(request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InsuranceCompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateInsuranceCompanyRequest request, CancellationToken ct)
    {
        var result = await _insuranceCompanies.UpdateAsync(id, request, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Activates or deactivates a company. There is no delete: a deactivated company
    /// disappears from the patient's request form while every request referencing it
    /// stays intact.
    /// </summary>
    [HttpPatch("{id:guid}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse<ToggleInsuranceCompanyStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
    {
        var result = await _insuranceCompanies.ToggleStatusAsync(id, ct);
        return FromResult(result);
    }
}
