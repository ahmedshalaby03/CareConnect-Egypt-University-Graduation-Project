using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.HospitalDiscovery;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>Hospital location-coverage counters for the SuperAdmin dashboard.</summary>
[Route("api/super-admin/hospitals")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
public class SuperAdminHospitalDiscoveryController : ApiControllerBase
{
    private readonly IHospitalDiscoveryService _discovery;

    public SuperAdminHospitalDiscoveryController(IHospitalDiscoveryService discovery) => _discovery = discovery;

    [HttpGet("dashboard-stats")]
    [ProducesResponseType(typeof(ApiResponse<SuperAdminHospitalLocationStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var result = await _discovery.GetSuperAdminLocationStatsAsync(ct);
        return FromResult(result);
    }
}
