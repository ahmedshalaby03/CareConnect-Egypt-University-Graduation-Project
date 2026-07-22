using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Appointments;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// The doctor's own appointments. The service checks that every appointment id belongs to
/// the calling doctor, so a doctor cannot manage another doctor's booking.
/// </summary>
[Route("api/doctor/appointments")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.DoctorOnly)]
public class DoctorAppointmentsController : ApiControllerBase
{
    private readonly IAppointmentService _appointments;

    public DoctorAppointmentsController(IAppointmentService appointments) => _appointments = appointments;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DoctorAppointmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] DoctorAppointmentQueryParameters query, CancellationToken ct)
    {
        var result = await _appointments.GetDoctorAppointmentsAsync(CurrentUserId, query, ct);
        return FromResult(result);
    }

    /// <summary>Feeds the today/pending/confirmed/completed widgets on the doctor dashboard.</summary>
    [HttpGet("dashboard-stats")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var result = await _appointments.GetDoctorDashboardStatsAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DoctorAppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _appointments.GetDoctorAppointmentByIdAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<DoctorAppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var result = await _appointments.ConfirmAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    /// <summary>Declines a pending appointment. A reason is mandatory and is shown to the patient.</summary>
    [HttpPatch("{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<DoctorAppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid id, RejectAppointmentRequest request, CancellationToken ct)
    {
        var result = await _appointments.RejectAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<DoctorAppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancelAppointmentRequest request, CancellationToken ct)
    {
        var result = await _appointments.CancelByDoctorAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse<DoctorAppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _appointments.CompleteAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/no-show")]
    [ProducesResponseType(typeof(ApiResponse<DoctorAppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkNoShow(Guid id, CancellationToken ct)
    {
        var result = await _appointments.MarkNoShowAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    /// <summary>Private clinical notes. Never returned to the Patient or Hospital roles.</summary>
    [HttpPut("{id:guid}/notes")]
    [ProducesResponseType(typeof(ApiResponse<DoctorAppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateNotes(Guid id, DoctorNotesRequest request, CancellationToken ct)
    {
        var result = await _appointments.UpdateNotesAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }
}
