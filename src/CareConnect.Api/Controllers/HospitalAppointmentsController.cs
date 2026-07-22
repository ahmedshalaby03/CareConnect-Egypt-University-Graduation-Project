using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Appointments;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// Read-only scheduling view for the hospital's own appointments. No DoctorNotes and no
/// patient medical-history detail are ever included in these responses.
/// </summary>
[Route("api/hospital/appointments")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.HospitalOnly)]
public class HospitalAppointmentsController : ApiControllerBase
{
    private readonly IAppointmentService _appointments;

    public HospitalAppointmentsController(IAppointmentService appointments) => _appointments = appointments;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<HospitalAppointmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] HospitalAppointmentQueryParameters query, CancellationToken ct)
    {
        var result = await _appointments.GetHospitalAppointmentsAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    /// <summary>Feeds the today/pending/active-doctors widgets on the hospital dashboard.</summary>
    [HttpGet("dashboard-stats")]
    [ProducesResponseType(typeof(ApiResponse<HospitalDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var result = await _appointments.GetHospitalDashboardStatsAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<HospitalAppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _appointments.GetHospitalAppointmentByIdAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }
}
