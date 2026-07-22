using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Affiliations;

namespace CareConnect.Application.Interfaces;

/// <summary>
/// Both sides of the doctor-hospital relationship. Every method takes the acting user's id
/// and resolves ownership from it, so a route id can never be used to act on somebody
/// else's profile.
/// </summary>
public interface IDoctorHospitalAffiliationService
{
    // ------------------------------------------------------------- Doctor side

    Task<Result<PagedResult<DoctorHospitalRequestDto>>> GetDoctorRequestsAsync(
        string doctorUserId,
        DoctorAffiliationQueryParameters query,
        CancellationToken ct = default);

    Task<Result<DoctorHospitalRequestDto>> CreateRequestAsync(
        string doctorUserId,
        CreateAffiliationRequest request,
        CancellationToken ct = default);

    Task<Result<DoctorHospitalRequestDto>> CancelRequestAsync(
        string doctorUserId,
        Guid requestId,
        CancellationToken ct = default);

    /// <summary>Marks one approved hospital as primary and clears the previous one.</summary>
    Task<Result<DoctorHospitalRequestDto>> SetPrimaryHospitalAsync(
        string doctorUserId,
        Guid hospitalProfileId,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<DoctorAffiliatedHospitalDto>>> GetDoctorHospitalsAsync(
        string doctorUserId,
        CancellationToken ct = default);

    // ----------------------------------------------------------- Hospital side

    Task<Result<PagedResult<HospitalDoctorRequestDto>>> GetHospitalRequestsAsync(
        string hospitalUserId,
        HospitalAffiliationQueryParameters query,
        CancellationToken ct = default);

    Task<Result<HospitalDoctorRequestDto>> ApproveRequestAsync(
        string hospitalUserId,
        Guid requestId,
        CancellationToken ct = default);

    Task<Result<HospitalDoctorRequestDto>> RejectRequestAsync(
        string hospitalUserId,
        Guid requestId,
        RejectAffiliationRequest request,
        CancellationToken ct = default);

    Task<Result<PagedResult<HospitalDoctorDto>>> GetHospitalDoctorsAsync(
        string hospitalUserId,
        HospitalAffiliationQueryParameters query,
        CancellationToken ct = default);

    /// <summary>Ends an approved affiliation by moving it to Removed; the row is kept.</summary>
    Task<Result<HospitalDoctorRequestDto>> RemoveDoctorAsync(
        string hospitalUserId,
        Guid doctorProfileId,
        CancellationToken ct = default);
}
