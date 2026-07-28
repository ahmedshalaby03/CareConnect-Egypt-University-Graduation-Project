using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceProviders;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/super-admin/medical-service-categories")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
public sealed class SuperAdminMedicalServiceCategoriesController : ApiControllerBase
{
    private readonly IMedicalServiceCategoryService _categories;

    public SuperAdminMedicalServiceCategoriesController(
        IMedicalServiceCategoryService categories) =>
        _categories = categories;

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<MedicalServiceCategoryDto>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] MedicalServiceCategoryQueryParameters query,
        CancellationToken ct)
    {
        var result = await _categories.GetAllAsync(query, ct);
        return FromResult(result);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<MedicalServiceCategoryDto>),
        StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        CreateMedicalServiceCategoryRequest request,
        CancellationToken ct)
    {
        var result = await _categories.CreateAsync(request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateMedicalServiceCategoryRequest request,
        CancellationToken ct)
    {
        var result = await _categories.UpdateAsync(id, request, ct);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        SetMedicalServiceCategoryStatusRequest request,
        CancellationToken ct)
    {
        var result = await _categories.SetStatusAsync(id, request, ct);
        return FromResult(result);
    }
}
