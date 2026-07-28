using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.MedicalServiceRequests;

namespace CareConnect.Application.Interfaces;

public interface IMedicalServiceRequestService
{
    Task<Result<MedicalServiceRequestDetailsDto>> CreateAsync(
        string patientUserId,
        CreateMedicalServiceRequestRequest request,
        CancellationToken ct = default);

    Task<Result<PagedResult<MedicalServiceRequestSummaryDto>>> GetPatientRequestsAsync(
        string patientUserId,
        PatientMedicalServiceRequestFilter filter,
        CancellationToken ct = default);

    Task<Result<MedicalServiceRequestDetailsDto>> GetPatientRequestByIdAsync(
        string patientUserId,
        Guid requestId,
        CancellationToken ct = default);

    Task<Result<MedicalServiceRequestDetailsDto>> CancelByPatientAsync(
        string patientUserId,
        Guid requestId,
        CancelMedicalServiceRequestRequest request,
        CancellationToken ct = default);

    Task<Result<PagedResult<MedicalServiceRequestSummaryDto>>> GetProviderRequestsAsync(
        string providerUserId,
        ProviderMedicalServiceRequestFilter filter,
        CancellationToken ct = default);

    Task<Result<MedicalServiceRequestDetailsDto>> GetProviderRequestByIdAsync(
        string providerUserId,
        Guid requestId,
        CancellationToken ct = default);

    Task<Result<MedicalServiceRequestDetailsDto>> AcceptAsync(
        string providerUserId,
        Guid requestId,
        AcceptMedicalServiceRequestRequest request,
        CancellationToken ct = default);

    Task<Result<MedicalServiceRequestDetailsDto>> RejectAsync(
        string providerUserId,
        Guid requestId,
        RejectMedicalServiceRequestRequest request,
        CancellationToken ct = default);

    Task<Result<MedicalServiceRequestDetailsDto>> CancelByProviderAsync(
        string providerUserId,
        Guid requestId,
        CancelMedicalServiceRequestRequest request,
        CancellationToken ct = default);

    Task<Result<MedicalServiceRequestDetailsDto>> CompleteAsync(
        string providerUserId,
        Guid requestId,
        CancellationToken ct = default);

    Task<Result<MedicalServiceRequestDashboardSummaryDto>> GetProviderDashboardSummaryAsync(
        string providerUserId,
        CancellationToken ct = default);
}
