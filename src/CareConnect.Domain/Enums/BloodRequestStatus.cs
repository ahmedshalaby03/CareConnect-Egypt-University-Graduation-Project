namespace CareConnect.Domain.Enums;

/// <summary>
/// Lifecycle of a patient's blood request. Explicit numeric values so a stored value never
/// silently shifts meaning if a case is inserted later.
/// </summary>
public enum BloodRequestStatus
{
    /// <summary>Submitted by the patient, awaiting the hospital's decision.</summary>
    Pending = 1,

    /// <summary>The hospital approved and allocated the requested units.</summary>
    Approved = 2,

    /// <summary>The hospital declined the request; a reason is always recorded.</summary>
    Rejected = 3,

    /// <summary>The approved request has been completed.</summary>
    Fulfilled = 4,

    /// <summary>Withdrawn by the patient before the hospital made a decision.</summary>
    Cancelled = 5
}
