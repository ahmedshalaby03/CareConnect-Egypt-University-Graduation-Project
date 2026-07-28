using CareConnect.Api.Common;
using CareConnect.Application.DTOs.Reviews;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api")]
[Authorize]
public sealed class PublicReviewsController : ApiControllerBase
{
    private readonly IRatingQueryService _ratings;
    public PublicReviewsController(IRatingQueryService ratings) => _ratings = ratings;

    [HttpGet("doctors/{id:guid}/reviews")]
    public async Task<IActionResult> DoctorReviews(Guid id, [FromQuery] ReviewListFilter filter, CancellationToken ct) =>
        FromResult(await _ratings.GetPublicReviewsAsync(ReviewType.Doctor, id, filter, ct));
    [HttpGet("doctors/{id:guid}/rating-summary")]
    public async Task<IActionResult> DoctorSummary(Guid id, CancellationToken ct) =>
        FromResult(await _ratings.GetPublicSummaryAsync(ReviewType.Doctor, id, ct));
    [HttpGet("hospitals/{id:guid}/reviews")]
    public async Task<IActionResult> HospitalReviews(Guid id, [FromQuery] ReviewListFilter filter, CancellationToken ct) =>
        FromResult(await _ratings.GetPublicReviewsAsync(ReviewType.Hospital, id, filter, ct));
    [HttpGet("hospitals/{id:guid}/rating-summary")]
    public async Task<IActionResult> HospitalSummary(Guid id, CancellationToken ct) =>
        FromResult(await _ratings.GetPublicSummaryAsync(ReviewType.Hospital, id, ct));
    [HttpGet("medical-service-providers/{id:guid}/reviews")]
    public async Task<IActionResult> ProviderReviews(Guid id, [FromQuery] ReviewListFilter filter, CancellationToken ct) =>
        FromResult(await _ratings.GetPublicReviewsAsync(ReviewType.MedicalServiceProvider, id, filter, ct));
    [HttpGet("medical-service-providers/{id:guid}/rating-summary")]
    public async Task<IActionResult> ProviderSummary(Guid id, CancellationToken ct) =>
        FromResult(await _ratings.GetPublicSummaryAsync(ReviewType.MedicalServiceProvider, id, ct));
}
