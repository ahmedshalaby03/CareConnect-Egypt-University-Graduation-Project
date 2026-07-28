using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Dashboards;

namespace CareConnect.Application.Interfaces;

public interface IDashboardService
{
    Task<Result<PatientDashboardDto>> GetPatientAsync(
        string currentUserId,
        CancellationToken cancellationToken = default);

    Task<Result<DoctorDashboardDto>> GetDoctorAsync(
        string currentUserId,
        CancellationToken cancellationToken = default);

    Task<Result<HospitalDashboardDto>> GetHospitalAsync(
        string currentUserId,
        CancellationToken cancellationToken = default);

    Task<Result<MedicalServiceProviderDashboardDto>> GetMedicalServiceProviderAsync(
        string currentUserId,
        CancellationToken cancellationToken = default);

    Task<Result<SuperAdminDashboardDto>> GetSuperAdminAsync(
        CancellationToken cancellationToken = default);
}
