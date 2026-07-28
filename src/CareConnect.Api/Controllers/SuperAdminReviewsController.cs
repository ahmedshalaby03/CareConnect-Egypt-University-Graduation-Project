using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Reviews;
using CareConnect.Application.Interfaces;
using CareConnect.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/super-admin/reviews")]
[Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
public sealed class SuperAdminReviewsController : ApiControllerBase
{
    private readonly IReviewModerationService _moderation;
    public SuperAdminReviewsController(IReviewModerationService moderation) => _moderation = moderation;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] SuperAdminReviewFilter filter, CancellationToken ct) =>
        FromResult(await _moderation.GetAllAsync(CurrentUserId, filter, ct));

    [HttpGet("{reviewType}/{id:guid}")]
    public async Task<IActionResult> GetById(string reviewType, Guid id, CancellationToken ct) =>
        TryType(reviewType, out var type)
            ? FromResult(await _moderation.GetByIdAsync(CurrentUserId, type, id, ct))
            : FromResult(Result<ReviewDto>.Invalid("Review type is invalid."));

    [HttpPost("{reviewType}/{id:guid}/hide")]
    public async Task<IActionResult> Hide(
        string reviewType, Guid id, ModerateReviewRequest request, CancellationToken ct) =>
        TryType(reviewType, out var type)
            ? FromResult(await _moderation.HideAsync(CurrentUserId, type, id, request, ct))
            : FromResult(Result<ReviewDto>.Invalid("Review type is invalid."));

    [HttpPost("{reviewType}/{id:guid}/restore")]
    public async Task<IActionResult> Restore(string reviewType, Guid id, CancellationToken ct) =>
        TryType(reviewType, out var type)
            ? FromResult(await _moderation.RestoreAsync(CurrentUserId, type, id, ct))
            : FromResult(Result<ReviewDto>.Invalid("Review type is invalid."));

    private static bool TryType(string value, out ReviewType type)
    {
        type = value.ToLowerInvariant() switch
        {
            "doctor" => ReviewType.Doctor,
            "hospital" => ReviewType.Hospital,
            "medical-service-provider" => ReviewType.MedicalServiceProvider,
            _ => 0
        };
        return type != 0;
    }
}
