using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.BloodRequests;

namespace CareConnect.Application.Interfaces;

/// <summary>
/// Both sides of a patient blood request - Patient and Hospital. Mirrors
/// <see cref="IInsuranceRequestService"/>: every method takes the acting user's id and
/// resolves ownership from it, so a route id can never reach another account's data.
/// </summary>
public interface IBloodRequestService
{
    // ------------------------------------------------------------- Patient side

    Task<Result<PagedResult<PatientBloodRequestDto>>> GetPatientRequestsAsync(
        string patientUserId,
        PatientBloodRequestQueryParameters query,
        CancellationToken ct = default);

    Task<Result<PatientBloodRequestDto>> GetPatientRequestByIdAsync(
        string patientUserId,
        Guid requestId,
        CancellationToken ct = default);

    /// <summary>
    /// Revalidates eligibility and the duplicate-request rule against fresh data
    /// immediately before saving, so a race between two submissions cannot slip through.
    /// Does not reduce stock - only approval does that.
    /// </summary>
    Task<Result<PatientBloodRequestDto>> CreateRequestAsync(
        string patientUserId,
        CreateBloodRequestRequest request,
        CancellationToken ct = default);

    /// <summary>Only a Pending request may be withdrawn.</summary>
    Task<Result<PatientBloodRequestDto>> CancelRequestAsync(
        string patientUserId,
        Guid requestId,
        CancellationToken ct = default);

    Task<Result<PatientBloodDashboardStatsDto>> GetPatientDashboardStatsAsync(
        string patientUserId,
        CancellationToken ct = default);

    // ------------------------------------------------------------ Hospital side

    Task<Result<PagedResult<HospitalBloodRequestDto>>> GetHospitalRequestsAsync(
        string hospitalUserId,
        HospitalBloodRequestQueryParameters query,
        CancellationToken ct = default);

    Task<Result<HospitalBloodRequestDto>> GetHospitalRequestByIdAsync(
        string hospitalUserId,
        Guid requestId,
        CancellationToken ct = default);

    /// <summary>
    /// Re-reads BloodStock, confirms enough AvailableUnits, decreases it and approves the
    /// request in one transaction. Returns 409 (via <see cref="ResultStatus.Conflict"/>)
    /// when stock is insufficient.
    /// </summary>
    Task<Result<HospitalBloodRequestDto>> ApproveAsync(
        string hospitalUserId,
        Guid requestId,
        ApproveBloodRequestRequest request,
        CancellationToken ct = default);

    Task<Result<HospitalBloodRequestDto>> RejectAsync(
        string hospitalUserId,
        Guid requestId,
        RejectBloodRequestRequest request,
        CancellationToken ct = default);

    /// <summary>Marks an Approved request Fulfilled. Does not touch stock again.</summary>
    Task<Result<HospitalBloodRequestDto>> FulfillAsync(
        string hospitalUserId,
        Guid requestId,
        CancellationToken ct = default);

    Task<Result<HospitalBloodRequestDto>> UpdateHospitalNotesAsync(
        string hospitalUserId,
        Guid requestId,
        BloodRequestHospitalNotesRequest request,
        CancellationToken ct = default);

    Task<Result<HospitalBloodDashboardStatsDto>> GetHospitalDashboardStatsAsync(
        string hospitalUserId,
        CancellationToken ct = default);
}
