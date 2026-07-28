using CareConnect.Api.Authorization;
using CareConnect.Api.Common;
using CareConnect.Application.DTOs.MedicalServiceProviders;
using CareConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnect.Api.Controllers;

[Route("api/medical-service-provider/services")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.MedicalServiceProviderOnly)]
public sealed class MedicalServiceProviderServicesController : ApiControllerBase
{
    private readonly IMedicalServiceProviderProfileService _profiles;

    public MedicalServiceProviderServicesController(
        IMedicalServiceProviderProfileService profiles) =>
        _profiles = profiles;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _profiles.GetServicesAsync(CurrentUserId, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _profiles.GetServiceAsync(CurrentUserId, id, ct);
        return FromResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateMedicalServiceOfferingRequest request,
        CancellationToken ct)
    {
        var result = await _profiles.CreateServiceAsync(CurrentUserId, request, ct);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateMedicalServiceOfferingRequest request,
        CancellationToken ct)
    {
        var result = await _profiles.UpdateServiceAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        SetMedicalServiceOfferingStatusRequest request,
        CancellationToken ct)
    {
        var result = await _profiles.SetServiceStatusAsync(CurrentUserId, id, request, ct);
        return FromResult(result);
    }
}
