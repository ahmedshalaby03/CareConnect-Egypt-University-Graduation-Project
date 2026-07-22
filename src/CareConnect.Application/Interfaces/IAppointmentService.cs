using CareConnect.Application.Common.Models;
using CareConnect.Application.DTOs.Appointments;

namespace CareConnect.Application.Interfaces;

/// <summary>
/// All three sides of an appointment - Patient, Doctor and Hospital. Every method takes the
/// acting user's id and resolves ownership from it, matching the pattern used by
/// <see cref="IDoctorHospitalAffiliationService"/>: a route id can never reach another
/// account's data.
/// </summary>
public interface IAppointmentService
{
    // ------------------------------------------------------------- Patient side

    Task<Result<PagedResult<PatientAppointmentDto>>> GetPatientAppointmentsAsync(
        string patientUserId,
        PatientAppointmentQueryParameters query,
        CancellationToken ct = default);

    Task<Result<PatientAppointmentDto>> GetPatientAppointmentByIdAsync(
        string patientUserId,
        Guid appointmentId,
        CancellationToken ct = default);

    /// <summary>
    /// Revalidates every booking rule against current data immediately before saving, inside
    /// a transaction, so a slot cannot be double-booked by two near-simultaneous requests.
    /// </summary>
    Task<Result<PatientAppointmentDto>> BookAppointmentAsync(
        string patientUserId,
        BookAppointmentRequest request,
        CancellationToken ct = default);

    Task<Result<PatientAppointmentDto>> CancelByPatientAsync(
        string patientUserId,
        Guid appointmentId,
        CancelAppointmentRequest request,
        CancellationToken ct = default);

    Task<Result<PatientDashboardStatsDto>> GetPatientDashboardStatsAsync(
        string patientUserId,
        CancellationToken ct = default);

    // -------------------------------------------------------------- Doctor side

    Task<Result<PagedResult<DoctorAppointmentDto>>> GetDoctorAppointmentsAsync(
        string doctorUserId,
        DoctorAppointmentQueryParameters query,
        CancellationToken ct = default);

    Task<Result<DoctorAppointmentDto>> GetDoctorAppointmentByIdAsync(
        string doctorUserId,
        Guid appointmentId,
        CancellationToken ct = default);

    Task<Result<DoctorAppointmentDto>> ConfirmAsync(
        string doctorUserId,
        Guid appointmentId,
        CancellationToken ct = default);

    Task<Result<DoctorAppointmentDto>> RejectAsync(
        string doctorUserId,
        Guid appointmentId,
        RejectAppointmentRequest request,
        CancellationToken ct = default);

    Task<Result<DoctorAppointmentDto>> CancelByDoctorAsync(
        string doctorUserId,
        Guid appointmentId,
        CancelAppointmentRequest request,
        CancellationToken ct = default);

    Task<Result<DoctorAppointmentDto>> CompleteAsync(
        string doctorUserId,
        Guid appointmentId,
        CancellationToken ct = default);

    Task<Result<DoctorAppointmentDto>> MarkNoShowAsync(
        string doctorUserId,
        Guid appointmentId,
        CancellationToken ct = default);

    Task<Result<DoctorAppointmentDto>> UpdateNotesAsync(
        string doctorUserId,
        Guid appointmentId,
        DoctorNotesRequest request,
        CancellationToken ct = default);

    Task<Result<DoctorDashboardStatsDto>> GetDoctorDashboardStatsAsync(
        string doctorUserId,
        CancellationToken ct = default);

    // ------------------------------------------------------------ Hospital side

    Task<Result<PagedResult<HospitalAppointmentDto>>> GetHospitalAppointmentsAsync(
        string hospitalUserId,
        HospitalAppointmentQueryParameters query,
        CancellationToken ct = default);

    Task<Result<HospitalAppointmentDto>> GetHospitalAppointmentByIdAsync(
        string hospitalUserId,
        Guid appointmentId,
        CancellationToken ct = default);

    Task<Result<HospitalDashboardStatsDto>> GetHospitalDashboardStatsAsync(
        string hospitalUserId,
        CancellationToken ct = default);
}
