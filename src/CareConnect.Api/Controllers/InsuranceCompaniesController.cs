using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.InsuranceCompanies;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The shared insurance company lookup. Any signed-in account may read it, because the
/// patient's request form needs the same active list SuperAdmin manages.
/// </summary>
[Route("api/insurance-companies")]
[Produces("application/json")]
[Authorize]
public class InsuranceCompaniesController : ApiControllerBase
{
    private readonly IInsuranceCompanyService _insuranceCompanies;

    public InsuranceCompaniesController(IInsuranceCompanyService insuranceCompanies) =>
        _insuranceCompanies = insuranceCompanies;

    /// <summary>Active insurance companies only, sorted alphabetically by name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InsuranceCompanyOptionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var result = await _insuranceCompanies.GetActiveAsync(ct);
        return FromResult(result);
    }
}
