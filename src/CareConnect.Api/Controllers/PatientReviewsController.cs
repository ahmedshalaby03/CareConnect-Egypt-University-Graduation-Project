using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.DTOs.Reviews;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/patient")]
[Authorize(Policy = AuthorizationPolicies.PatientOnly)]
public sealed class PatientReviewsController : ApiControllerBase
{
    private readonly IReviewService _reviews;
    public PatientReviewsController(IReviewService reviews) => _reviews = reviews;

    [HttpGet("appointments/{sourceId:guid}/doctor-review/eligibility")]
    public async Task<IActionResult> DoctorEligibility(Guid sourceId, CancellationToken ct) =>
        FromResult(await _reviews.GetEligibilityAsync(CurrentUserId, ReviewType.Doctor, sourceId, ct));
    [HttpGet("appointments/{sourceId:guid}/doctor-review")]
    public async Task<IActionResult> GetDoctor(Guid sourceId, CancellationToken ct) =>
        FromResult(await _reviews.GetPatientReviewAsync(CurrentUserId, ReviewType.Doctor, sourceId, ct));
    [HttpPost("appointments/{sourceId:guid}/doctor-review")]
    public async Task<IActionResult> CreateDoctor(Guid sourceId, SaveReviewRequest request, CancellationToken ct) =>
        FromResult(await _reviews.CreateAsync(CurrentUserId, ReviewType.Doctor, sourceId, request, ct), 201);
    [HttpPut("appointments/{sourceId:guid}/doctor-review")]
    public async Task<IActionResult> UpdateDoctor(Guid sourceId, SaveReviewRequest request, CancellationToken ct) =>
        FromResult(await _reviews.UpdateAsync(CurrentUserId, ReviewType.Doctor, sourceId, request, ct));

    [HttpGet("appointments/{sourceId:guid}/hospital-review/eligibility")]
    public async Task<IActionResult> HospitalEligibility(Guid sourceId, CancellationToken ct) =>
        FromResult(await _reviews.GetEligibilityAsync(CurrentUserId, ReviewType.Hospital, sourceId, ct));
    [HttpGet("appointments/{sourceId:guid}/hospital-review")]
    public async Task<IActionResult> GetHospital(Guid sourceId, CancellationToken ct) =>
        FromResult(await _reviews.GetPatientReviewAsync(CurrentUserId, ReviewType.Hospital, sourceId, ct));
    [HttpPost("appointments/{sourceId:guid}/hospital-review")]
    public async Task<IActionResult> CreateHospital(Guid sourceId, SaveReviewRequest request, CancellationToken ct) =>
        FromResult(await _reviews.CreateAsync(CurrentUserId, ReviewType.Hospital, sourceId, request, ct), 201);
    [HttpPut("appointments/{sourceId:guid}/hospital-review")]
    public async Task<IActionResult> UpdateHospital(Guid sourceId, SaveReviewRequest request, CancellationToken ct) =>
        FromResult(await _reviews.UpdateAsync(CurrentUserId, ReviewType.Hospital, sourceId, request, ct));

    [HttpGet("medical-service-requests/{sourceId:guid}/review/eligibility")]
    public async Task<IActionResult> ProviderEligibility(Guid sourceId, CancellationToken ct) =>
        FromResult(await _reviews.GetEligibilityAsync(CurrentUserId, ReviewType.MedicalServiceProvider, sourceId, ct));
    [HttpGet("medical-service-requests/{sourceId:guid}/review")]
    public async Task<IActionResult> GetProvider(Guid sourceId, CancellationToken ct) =>
        FromResult(await _reviews.GetPatientReviewAsync(CurrentUserId, ReviewType.MedicalServiceProvider, sourceId, ct));
    [HttpPost("medical-service-requests/{sourceId:guid}/review")]
    public async Task<IActionResult> CreateProvider(Guid sourceId, SaveReviewRequest request, CancellationToken ct) =>
        FromResult(await _reviews.CreateAsync(CurrentUserId, ReviewType.MedicalServiceProvider, sourceId, request, ct), 201);
    [HttpPut("medical-service-requests/{sourceId:guid}/review")]
    public async Task<IActionResult> UpdateProvider(Guid sourceId, SaveReviewRequest request, CancellationToken ct) =>
        FromResult(await _reviews.UpdateAsync(CurrentUserId, ReviewType.MedicalServiceProvider, sourceId, request, ct));

    [HttpGet("reviews")]
    public async Task<IActionResult> GetAll([FromQuery] PatientReviewFilter filter, CancellationToken ct) =>
        FromResult(await _reviews.GetPatientReviewsAsync(CurrentUserId, filter, ct));
}
