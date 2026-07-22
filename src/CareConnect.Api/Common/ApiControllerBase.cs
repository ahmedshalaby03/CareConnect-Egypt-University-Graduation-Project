using CareConnect.Application.Common.Models;
using CareConnect.Domain.Constants;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Common;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The authenticated caller's id. Only read this on endpoints marked [Authorize].</summary>
    protected string CurrentUserId =>
        User.FindFirst(AppClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("The request is missing an authenticated user id.");

    /// <summary>
    /// Turns a service Result into an HTTP response, so every endpoint answers with the
    /// same envelope and the status code always matches the failure reason.
    /// </summary>
    protected IActionResult FromResult<T>(Result<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.Succeeded)
        {
            return StatusCode(successStatusCode, ApiResponse<T>.Ok(result.Data, result.Message));
        }

        var statusCode = result.Status switch
        {
            ResultStatus.ValidationFailed => StatusCodes.Status400BadRequest,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(statusCode, ApiResponse<T>.Fail(result.Message, result.Errors));
    }
}
