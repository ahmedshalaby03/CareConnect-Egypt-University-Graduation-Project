using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Directory;

namespace CareConnect.Application.Interfaces;

/// <summary>
/// Read-only browse endpoints. Any authenticated user may call these regardless of role,
/// and they only ever expose data a profile owner chose to publish.
/// </summary>
public interface IHealthcareDirectoryService
{
    Task<Result<PagedResult<HospitalDirectoryItemDto>>> SearchHospitalsAsync(
        HospitalDirectoryQueryParameters query,
        CancellationToken ct = default);

    Task<Result<HospitalDirectoryDetailsDto>> GetHospitalAsync(Guid id, CancellationToken ct = default);

    Task<Result<PagedResult<DoctorDirectoryItemDto>>> SearchDoctorsAsync(
        DoctorDirectoryQueryParameters query,
        CancellationToken ct = default);

    Task<Result<DoctorDirectoryDetailsDto>> GetDoctorAsync(Guid id, CancellationToken ct = default);
}
