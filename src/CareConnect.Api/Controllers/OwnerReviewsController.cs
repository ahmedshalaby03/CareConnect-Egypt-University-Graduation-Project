using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.DTOs.Reviews;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/doctor/reviews")]
[Authorize(Policy = AuthorizationPolicies.DoctorOnly)]
public sealed class DoctorReviewsController : ApiControllerBase
{
    private readonly IRatingQueryService _ratings;
    public DoctorReviewsController(IRatingQueryService ratings) => _ratings = ratings;
    [HttpGet] public async Task<IActionResult> Get([FromQuery] ReviewListFilter filter, CancellationToken ct) =>
        FromResult(await _ratings.GetOwnerReviewsAsync(CurrentUserId, ReviewType.Doctor, filter, ct));
    [HttpGet("summary")] public async Task<IActionResult> Summary(CancellationToken ct) =>
        FromResult(await _ratings.GetOwnerSummaryAsync(CurrentUserId, ReviewType.Doctor, ct));
}

[Route("api/hospital/reviews")]
[Authorize(Policy = AuthorizationPolicies.HospitalOnly)]
public sealed class HospitalReviewsController : ApiControllerBase
{
    private readonly IRatingQueryService _ratings;
    public HospitalReviewsController(IRatingQueryService ratings) => _ratings = ratings;
    [HttpGet] public async Task<IActionResult> Get([FromQuery] ReviewListFilter filter, CancellationToken ct) =>
        FromResult(await _ratings.GetOwnerReviewsAsync(CurrentUserId, ReviewType.Hospital, filter, ct));
    [HttpGet("summary")] public async Task<IActionResult> Summary(CancellationToken ct) =>
        FromResult(await _ratings.GetOwnerSummaryAsync(CurrentUserId, ReviewType.Hospital, ct));
}

[Route("api/medical-service-provider/reviews")]
[Authorize(Policy = AuthorizationPolicies.MedicalServiceProviderOnly)]
public sealed class MedicalServiceProviderReviewsController : ApiControllerBase
{
    private readonly IRatingQueryService _ratings;
    public MedicalServiceProviderReviewsController(IRatingQueryService ratings) => _ratings = ratings;
    [HttpGet] public async Task<IActionResult> Get([FromQuery] ReviewListFilter filter, CancellationToken ct) =>
        FromResult(await _ratings.GetOwnerReviewsAsync(CurrentUserId, ReviewType.MedicalServiceProvider, filter, ct));
    [HttpGet("summary")] public async Task<IActionResult> Summary(CancellationToken ct) =>
        FromResult(await _ratings.GetOwnerSummaryAsync(CurrentUserId, ReviewType.MedicalServiceProvider, ct));
}
