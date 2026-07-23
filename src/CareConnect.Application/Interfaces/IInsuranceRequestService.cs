using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.InsuranceRequests;

namespace CareConnect.Application.Interfaces;

/// <summary>
/// Both sides of a digital insurance request - Patient and Hospital. Mirrors
/// <see cref="IAppointmentService"/>: every method takes the acting user's id and resolves
/// ownership from it, so a route id can never reach another account's data.
/// </summary>
public interface IInsuranceRequestService
{
    // ------------------------------------------------------------- Patient side

    Task<Result<PagedResult<PatientInsuranceRequestDto>>> GetPatientRequestsAsync(
        string patientUserId,
        PatientInsuranceRequestQueryParameters query,
        CancellationToken ct = default);

    Task<Result<PatientInsuranceRequestDto>> GetPatientRequestByIdAsync(
        string patientUserId,
        Guid requestId,
        CancellationToken ct = default);

    /// <summary>
    /// Revalidates eligibility and the duplicate-request rule against fresh data
    /// immediately before saving, so a race between two submissions cannot slip through.
    /// </summary>
    Task<Result<PatientInsuranceRequestDto>> CreateRequestAsync(
        string patientUserId,
        CreateInsuranceRequestRequest request,
        CancellationToken ct = default);

    /// <summary>Only a Pending request may be withdrawn.</summary>
    Task<Result<PatientInsuranceRequestDto>> CancelRequestAsync(
        string patientUserId,
        Guid requestId,
        CancellationToken ct = default);

    Task<Result<PatientInsuranceDashboardStatsDto>> GetPatientDashboardStatsAsync(
        string patientUserId,
        CancellationToken ct = default);

    // ------------------------------------------------------------ Hospital side

    Task<Result<PagedResult<HospitalInsuranceRequestDto>>> GetHospitalRequestsAsync(
        string hospitalUserId,
        HospitalInsuranceRequestQueryParameters query,
        CancellationToken ct = default);

    Task<Result<HospitalInsuranceRequestDto>> GetHospitalRequestByIdAsync(
        string hospitalUserId,
        Guid requestId,
        CancellationToken ct = default);

    Task<Result<HospitalInsuranceRequestDto>> StartReviewAsync(
        string hospitalUserId,
        Guid requestId,
        CancellationToken ct = default);

    Task<Result<HospitalInsuranceRequestDto>> ApproveAsync(
        string hospitalUserId,
        Guid requestId,
        ApproveInsuranceRequestRequest request,
        CancellationToken ct = default);

    Task<Result<HospitalInsuranceRequestDto>> RejectAsync(
        string hospitalUserId,
        Guid requestId,
        RejectInsuranceRequestRequest request,
        CancellationToken ct = default);

    Task<Result<HospitalInsuranceRequestDto>> UpdateHospitalNotesAsync(
        string hospitalUserId,
        Guid requestId,
        InsuranceHospitalNotesRequest request,
        CancellationToken ct = default);

    Task<Result<HospitalInsuranceDashboardStatsDto>> GetHospitalDashboardStatsAsync(
        string hospitalUserId,
        CancellationToken ct = default);
}
