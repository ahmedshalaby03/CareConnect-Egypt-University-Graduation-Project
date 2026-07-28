using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Api.Extensions;
using CareConnect.Application.DTOs.AiMedicalAssistant;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/ai-medical-assistant")]
[Authorize(Policy = AuthorizationPolicies.PatientOnly)]
[EnableRateLimiting(RateLimitingExtensions.AiMedicalAssistantPolicy)]
public sealed class AiMedicalAssistantController : ApiControllerBase
{
    private readonly IAiMedicalAssistantService _assistant;

    public AiMedicalAssistantController(IAiMedicalAssistantService assistant)
    {
        _assistant = assistant;
    }

    [HttpPost("chat")]
    [ProducesResponseType(typeof(MedicalAssistantChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Chat(
        [FromBody] MedicalAssistantChatRequest request,
        CancellationToken ct)
    {
        var result = await _assistant.ChatAsync(CurrentUserId, request, ct);
        return FromResult(result);
    }
}
