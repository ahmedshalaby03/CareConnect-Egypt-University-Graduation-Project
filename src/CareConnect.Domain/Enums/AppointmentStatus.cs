namespace CareConnect.Domain.Enums;

/// <summary>
/// Lifecycle of a booked appointment. Explicit numeric values so a stored value never
/// silently shifts meaning if a case is inserted later.
/// </summary>
public enum AppointmentStatus
{
    /// <summary>Booked by the patient, awaiting the doctor's decision.</summary>
    Pending = 1,

    /// <summary>The doctor accepted the booking.</summary>
    Confirmed = 2,

    /// <summary>The doctor declined; a reason is always recorded.</summary>
    Rejected = 3,

    /// <summary>Withdrawn by the patient or the doctor before it took place.</summary>
    Cancelled = 4,

    /// <summary>The visit happened.</summary>
    Completed = 5,

    /// <summary>The patient did not show up for a confirmed appointment.</summary>
    NoShow = 6
}
