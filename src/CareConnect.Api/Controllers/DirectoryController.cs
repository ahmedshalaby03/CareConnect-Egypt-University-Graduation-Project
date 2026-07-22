using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Directory;
using CareConnect.Application.DTOs.Scheduling;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

/// <summary>
/// Browse hospitals. Authenticated but deliberately not role-restricted: patients, doctors
/// and hospitals all use the same directory. Only completed profiles on active accounts
/// appear, and only approved doctors are listed against a hospital.
/// </summary>
[Route("api/hospitals")]
[Produces("application/json")]
[Authorize]
public class HospitalsDirectoryController : ApiControllerBase
{
    private readonly IHealthcareDirectoryService _directory;

    public HospitalsDirectoryController(IHealthcareDirectoryService directory) => _directory = directory;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<HospitalDirectoryItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] HospitalDirectoryQueryParameters query,
        CancellationToken ct)
    {
        var result = await _directory.SearchHospitalsAsync(query, ct);
        return FromResult(result);
    }

    /// <summary>Full hospital profile, its specialties, and its approved doctors.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<HospitalDirectoryDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _directory.GetHospitalAsync(id, ct);
        return FromResult(result);
    }
}

/// <summary>
/// Browse doctors. Same access rules as the hospital directory: authenticated, any role,
/// completed profiles on active accounts only.
/// </summary>
[Route("api/doctors")]
[Produces("application/json")]
[Authorize]
public class DoctorsDirectoryController : ApiControllerBase
{
    private readonly IHealthcareDirectoryService _directory;
    private readonly IAvailableSlotService _slots;

    public DoctorsDirectoryController(IHealthcareDirectoryService directory, IAvailableSlotService slots)
    {
        _directory = directory;
        _slots = slots;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DoctorDirectoryItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] DoctorDirectoryQueryParameters query,
        CancellationToken ct)
    {
        var result = await _directory.SearchDoctorsAsync(query, ct);
        return FromResult(result);
    }

    /// <summary>Full public doctor details and the hospitals they are approved at.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DoctorDirectoryDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _directory.GetDoctorAsync(id, ct);
        return FromResult(result);
    }

    /// <summary>
    /// Bookable slots for one doctor, at one hospital, on one date. Slot generation is
    /// always computed here, on the server - Angular never calculates a slot itself.
    /// </summary>
    [HttpGet("{doctorProfileId:guid}/available-slots")]
    [ProducesResponseType(typeof(ApiResponse<AvailableSlotsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableSlots(
        Guid doctorProfileId,
        [FromQuery] Guid hospitalProfileId,
        [FromQuery] DateOnly date,
        CancellationToken ct)
    {
        var result = await _slots.GetAvailableSlotsAsync(doctorProfileId, hospitalProfileId, date, ct);
        return FromResult(result);
    }
}
