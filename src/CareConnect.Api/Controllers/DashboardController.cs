using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Dashboards;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/dashboard")]
[Produces("application/json")]
[Authorize]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet("patient")]
    [Authorize(Policy = AuthorizationPolicies.PatientOnly)]
    [ProducesResponseType(typeof(ApiResponse<PatientDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatient(CancellationToken cancellationToken)
    {
        var result = await _dashboard.GetPatientAsync(CurrentUserId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("doctor")]
    [Authorize(Policy = AuthorizationPolicies.DoctorOnly)]
    [ProducesResponseType(typeof(ApiResponse<DoctorDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctor(CancellationToken cancellationToken)
    {
        var result = await _dashboard.GetDoctorAsync(CurrentUserId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("hospital")]
    [Authorize(Policy = AuthorizationPolicies.HospitalOnly)]
    [ProducesResponseType(typeof(ApiResponse<HospitalDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHospital(CancellationToken cancellationToken)
    {
        var result = await _dashboard.GetHospitalAsync(CurrentUserId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("medical-service-provider")]
    [Authorize(Policy = AuthorizationPolicies.MedicalServiceProviderOnly)]
    [ProducesResponseType(
        typeof(ApiResponse<MedicalServiceProviderDashboardDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedicalServiceProvider(
        CancellationToken cancellationToken)
    {
        var result = await _dashboard.GetMedicalServiceProviderAsync(
            CurrentUserId,
            cancellationToken);
        return FromResult(result);
    }

    [HttpGet("super-admin")]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<SuperAdminDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuperAdmin(CancellationToken cancellationToken)
    {
        var result = await _dashboard.GetSuperAdminAsync(cancellationToken);
        return FromResult(result);
    }
}
