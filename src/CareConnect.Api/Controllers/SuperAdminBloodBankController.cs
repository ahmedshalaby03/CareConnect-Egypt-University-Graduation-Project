using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.BloodRequests;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>Platform-wide blood-bank counters for the SuperAdmin dashboard.</summary>
[Route("api/super-admin/blood-bank")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
public class SuperAdminBloodBankController : ApiControllerBase
{
    private readonly IBloodStockService _bloodStock;

    public SuperAdminBloodBankController(IBloodStockService bloodStock) => _bloodStock = bloodStock;

    [HttpGet("dashboard-stats")]
    [ProducesResponseType(typeof(ApiResponse<SuperAdminBloodDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var result = await _bloodStock.GetSuperAdminDashboardStatsAsync(ct);
        return FromResult(result);
    }
}
