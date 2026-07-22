using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Appointments;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The patient's own appointments. No profile id in any route: ownership always comes from
/// the authenticated account, so a patient cannot reach another patient's booking.
/// </summary>
[Route("api/patient/appointments")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.PatientOnly)]
public class PatientAppointmentsController : ApiControllerBase
{
    private readonly IAppointmentService _appointments;

    public PatientAppointmentsController(IAppointmentService appointments) => _appointments = appointments;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientAppointmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] PatientAppointmentQueryParameters query, CancellationToken ct)
    {
        var result = await _appointments.GetPatientAppointmentsAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    /// <summary>Feeds the "next appointment" widget on the patient dashboard.</summary>
    [HttpGet("dashboard-stats")]
    [ProducesResponseType(typeof(ApiResponse<PatientDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var result = await _appointments.GetPatientDashboardStatsAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PatientAppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _appointments.GetPatientAppointmentByIdAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Every booking rule is revalidated against fresh data inside a transaction right
    /// before saving, so nothing here trusts what Angular displayed as "available".
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PatientAppointmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book(BookAppointmentRequest request, CancellationToken ct)
    {
        var result = await _appointments.BookAppointmentAsync(CurrentUserId, request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Cancels the patient's own Pending or Confirmed appointment. A reason is required.</summary>
    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PatientAppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancelAppointmentRequest request, CancellationToken ct)
    {
        var result = await _appointments.CancelByPatientAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }
}
