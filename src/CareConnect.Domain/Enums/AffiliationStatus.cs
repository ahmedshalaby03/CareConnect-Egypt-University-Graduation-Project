namespace CareConnect.Domain.Enums;

/// <summary>
/// Lifecycle of a doctor's request to work at a hospital. Records are never deleted, so a
/// terminal status is always the end state rather than a missing row.
/// </summary>
public enum AffiliationStatus
{
    /// <summary>Submitted by the doctor, awaiting the hospital's decision.</summary>
    Pending = 0,

    /// <summary>The hospital accepted the doctor.</summary>
    Approved = 1,

    /// <summary>The hospital declined; a reason is always recorded.</summary>
    Rejected = 2,

    /// <summary>The doctor withdrew the request before it was reviewed.</summary>
    Cancelled = 3,

    /// <summary>The hospital ended a previously approved affiliation.</summary>
    Removed = 4
}
