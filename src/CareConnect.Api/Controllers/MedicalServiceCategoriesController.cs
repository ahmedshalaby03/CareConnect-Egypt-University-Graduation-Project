using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceProviders;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/medical-service-categories")]
[Produces("application/json")]
[Authorize]
public sealed class MedicalServiceCategoriesController : ApiControllerBase
{
    private readonly IMedicalServiceCategoryService _categories;

    public MedicalServiceCategoriesController(IMedicalServiceCategoryService categories) =>
        _categories = categories;

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<MedicalServiceCategoryOptionDto>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var result = await _categories.GetActiveAsync(ct);
        return FromResult(result);
    }
}
