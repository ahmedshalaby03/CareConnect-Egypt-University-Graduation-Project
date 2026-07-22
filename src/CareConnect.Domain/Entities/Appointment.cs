using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

/// <summary>
/// A booked visit. Rows are never deleted - every transition (confirm, reject, cancel,
/// complete, no-show) moves <see cref="Status"/> forward and stamps the matching audit
/// timestamp, so the full history stays queryable.
/// </summary>
public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PatientProfileId { get; set; }
    public PatientProfile? PatientProfile { get; set; }

    public Guid DoctorProfileId { get; set; }
    public DoctorProfile? DoctorProfile { get; set; }

    public Guid HospitalProfileId { get; set; }
    public HospitalProfile? HospitalProfile { get; set; }

    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }

    /// <summary>Computed server-side from the matching DoctorAvailability slot duration.</summary>
    public TimeOnly EndTime { get; set; }

    public string? Reason { get; set; }
    public string? PatientNotes { get; set; }

    /// <summary>Visible to the doctor only - never returned to Patient or Hospital roles.</summary>
    public string? DoctorNotes { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }

    /// <summary>The user (patient or doctor) who cancelled the appointment.</summary>
    public string? CancelledByUserId { get; set; }
    public ApplicationUser? CancelledByUser { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// True for Pending and Confirmed appointments, the two statuses that hold a slot -
    /// see booking rules 15 and 16. Not mapped; queries inline this condition so it stays
    /// translatable by EF Core.
    /// </summary>
    public bool BlocksSlot => Status is AppointmentStatus.Pending or AppointmentStatus.Confirmed;
}
