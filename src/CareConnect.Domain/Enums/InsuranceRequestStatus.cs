namespace CareConnect.Domain.Enums;

/// <summary>
/// Lifecycle of a patient's digital insurance request. Explicit numeric values so a stored
/// value never silently shifts meaning if a case is inserted later.
/// </summary>
public enum InsuranceRequestStatus
{
    /// <summary>Submitted by the patient, awaiting the hospital's first look.</summary>
    Pending = 1,

    /// <summary>The hospital has started reviewing the request.</summary>
    UnderReview = 2,

    /// <summary>The hospital approved the request.</summary>
    Approved = 3,

    /// <summary>The hospital declined the request; a reason is always recorded.</summary>
    Rejected = 4,

    /// <summary>Withdrawn by the patient before a final decision.</summary>
    Cancelled = 5
}
