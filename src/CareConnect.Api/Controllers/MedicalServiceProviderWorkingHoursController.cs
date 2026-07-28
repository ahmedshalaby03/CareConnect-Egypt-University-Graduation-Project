using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.DTOs.MedicalServiceProviders;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/medical-service-provider/working-hours")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.MedicalServiceProviderOnly)]
public sealed class MedicalServiceProviderWorkingHoursController : ApiControllerBase
{
    private readonly IMedicalServiceProviderProfileService _profiles;

    public MedicalServiceProviderWorkingHoursController(
        IMedicalServiceProviderProfileService profiles) =>
        _profiles = profiles;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _profiles.GetWorkingHoursAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        UpdateMedicalServiceProviderWorkingHoursRequest request,
        CancellationToken ct)
    {
        var result = await _profiles.UpdateWorkingHoursAsync(CurrentUserId, request, ct);
        return FromResult(result);
    }
}
