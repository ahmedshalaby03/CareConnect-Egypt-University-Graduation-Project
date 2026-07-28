using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceProviders;

namespace CareConnect.Application.Interfaces;

public interface IMedicalServiceProviderProfileService
{
    Task<Result<MedicalServiceProviderProfileDto>> GetProfileAsync(
        string userId,
        CancellationToken ct = default);
    Task<Result<MedicalServiceProviderProfileDto>> UpdateProfileAsync(
        string userId,
        UpdateMedicalServiceProviderProfileRequest request,
        CancellationToken ct = default);
    Task<Result<MedicalServiceProviderProfileDto>> SetPublicationAsync(
        string userId,
        PublishMedicalServiceProviderProfileRequest request,
        CancellationToken ct = default);
    Task<Result<MedicalServiceProviderPreviewDto>> GetPreviewAsync(
        string userId,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<MedicalServiceOfferingDto>>> GetServicesAsync(
        string userId,
        CancellationToken ct = default);
    Task<Result<MedicalServiceOfferingDto>> GetServiceAsync(
        string userId,
        Guid serviceId,
        CancellationToken ct = default);
    Task<Result<MedicalServiceOfferingDto>> CreateServiceAsync(
        string userId,
        CreateMedicalServiceOfferingRequest request,
        CancellationToken ct = default);
    Task<Result<MedicalServiceOfferingDto>> UpdateServiceAsync(
        string userId,
        Guid serviceId,
        UpdateMedicalServiceOfferingRequest request,
        CancellationToken ct = default);
    Task<Result<MedicalServiceOfferingDto>> SetServiceStatusAsync(
        string userId,
        Guid serviceId,
        SetMedicalServiceOfferingStatusRequest request,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<MedicalServiceProviderWorkingHourDto>>> GetWorkingHoursAsync(
        string userId,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<MedicalServiceProviderWorkingHourDto>>> UpdateWorkingHoursAsync(
        string userId,
        UpdateMedicalServiceProviderWorkingHoursRequest request,
        CancellationToken ct = default);
}
