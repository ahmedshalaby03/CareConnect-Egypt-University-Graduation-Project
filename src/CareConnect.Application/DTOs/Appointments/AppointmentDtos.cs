using CareConnect.Application.Common.Models;
using CareConnect.Domain.Enums;

namespace CareConnect.Application.DTOs.Appointments;

// -------------------------------------------------------------------- Booking

public class BookAppointmentRequest
{
    public Guid DoctorProfileId { get; set; }
    public Guid HospitalProfileId { get; set; }
    public DateOnly AppointmentDate { get; set; }

    /// <summary>"HH:mm" or "HH:mm:ss" - must match a slot returned by the available-slots endpoint.</summary>
    public string StartTime { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
    public string? PatientNotes { get; set; }
}

public class RejectAppointmentRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}

public class CancelAppointmentRequest
{
    public string CancellationReason { get; set; } = string.Empty;
}

public class DoctorNotesRequest
{
    public string? DoctorNotes { get; set; }
}

// -------------------------------------------------------------------- Queries

public class PatientAppointmentQueryParameters : PagedQueryParameters
{
    public AppointmentStatus? Status { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? DoctorName { get; set; }
    public string? HospitalName { get; set; }
}

public class DoctorAppointmentQueryParameters : PagedQueryParameters
{
    public AppointmentStatus? Status { get; set; }
    public DateOnly? Date { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public Guid? HospitalProfileId { get; set; }
    public string? PatientName { get; set; }
}

public class HospitalAppointmentQueryParameters : PagedQueryParameters
{
    public AppointmentStatus? Status { get; set; }
    public DateOnly? Date { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public Guid? DoctorProfileId { get; set; }
    public string? DoctorName { get; set; }
    public string? PatientName { get; set; }
}

// ----------------------------------------------------------------- Responses

/// <summary>Shape returned to the Patient - no DoctorNotes, no unrelated private data.</summary>
public class PatientAppointmentDto
{
    public Guid AppointmentId { get; init; }
    public DateOnly AppointmentDate { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;

    public AppointmentStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;

    public string? Reason { get; init; }
    public string? PatientNotes { get; init; }

    public Guid DoctorProfileId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string? DoctorSpecialty { get; init; }

    public Guid HospitalProfileId { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string? HospitalAddress { get; init; }

    public string? RejectionReason { get; init; }
    public string? CancellationReason { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Shape returned to the Doctor - carries PatientNotes and DoctorNotes, plus just enough
/// patient identity to run the visit. Never a password hash, security stamp or refresh token.
/// </summary>
public class DoctorAppointmentDto
{
    public Guid AppointmentId { get; init; }
    public DateOnly AppointmentDate { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;

    public AppointmentStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;

    public string? Reason { get; init; }
    public string? PatientNotes { get; init; }
    public string? DoctorNotes { get; init; }

    public Guid PatientProfileId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string? PatientPhoneNumber { get; init; }

    public Guid HospitalProfileId { get; init; }
    public string HospitalName { get; init; } = string.Empty;

    public string? RejectionReason { get; init; }
    public string? CancellationReason { get; init; }

    public DateTime? ConfirmedAt { get; init; }
    public DateTime? RejectedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Shape returned to the Hospital - read-only scheduling view. No DoctorNotes, no patient
/// medical-history detail.
/// </summary>
public class HospitalAppointmentDto
{
    public Guid AppointmentId { get; init; }
    public DateOnly AppointmentDate { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;

    public AppointmentStatus Status { get; init; }
    public string StatusName { get; init; } = string.Empty;

    public string? Reason { get; init; }

    public Guid DoctorProfileId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string? DoctorSpecialty { get; init; }

    public string PatientName { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}

// -------------------------------------------------------------- Dashboard stats

public class PatientDashboardStatsDto
{
    public PatientAppointmentDto? NextAppointment { get; init; }
    public int UpcomingCount { get; init; }
    public int PendingCount { get; init; }
}

public class DoctorDashboardStatsDto
{
    public int TodayCount { get; init; }
    public int PendingCount { get; init; }
    public int ConfirmedCount { get; init; }
    public int CompletedThisMonthCount { get; init; }
}

public class HospitalDashboardStatsDto
{
    public int TodayCount { get; init; }
    public int PendingCount { get; init; }
    public int ActiveApprovedDoctorsCount { get; init; }
}
