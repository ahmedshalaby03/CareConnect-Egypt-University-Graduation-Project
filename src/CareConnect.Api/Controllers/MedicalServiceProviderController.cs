using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.DTOs.MedicalServiceProviders;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/medical-service-provider")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.MedicalServiceProviderOnly)]
public sealed class MedicalServiceProviderController : ApiControllerBase
{
    private readonly IMedicalServiceProviderProfileService _profiles;

    public MedicalServiceProviderController(
        IMedicalServiceProviderProfileService profiles) =>
        _profiles = profiles;

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await _profiles.GetProfileAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        UpdateMedicalServiceProviderProfileRequest request,
        CancellationToken ct)
    {
        var result = await _profiles.UpdateProfileAsync(CurrentUserId, request, ct);
        return FromResult(result);
    }

    [HttpPatch("profile/publication")]
    public async Task<IActionResult> SetPublication(
        PublishMedicalServiceProviderProfileRequest request,
        CancellationToken ct)
    {
        var result = await _profiles.SetPublicationAsync(CurrentUserId, request, ct);
        return FromResult(result);
    }

    [HttpGet("preview")]
    public async Task<IActionResult> GetPreview(CancellationToken ct)
    {
        var result = await _profiles.GetPreviewAsync(CurrentUserId, ct);
        return FromResult(result);
    }
}
