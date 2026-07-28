using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceProviders;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/medical-service-providers")]
[Produces("application/json")]
[Authorize]
public sealed class MedicalServiceProvidersController : ApiControllerBase
{
    private readonly IMedicalServiceProviderDirectoryService _directory;

    public MedicalServiceProvidersController(
        IMedicalServiceProviderDirectoryService directory) =>
        _directory = directory;

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<MedicalServiceProviderSummaryDto>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] MedicalServiceProviderFilter filter,
        CancellationToken ct)
    {
        var result = await _directory.SearchAsync(filter, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromQuery] MedicalServiceProviderDetailsQuery query,
        CancellationToken ct)
    {
        var result = await _directory.GetByIdAsync(id, query, ct);
        return FromResult(result);
    }
}
