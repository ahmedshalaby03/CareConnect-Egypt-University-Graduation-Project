using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

/// <summary>
/// A doctor's request to work at a hospital, and the record of that working relationship.
///
/// Rows are never deleted: cancelling, rejecting and removing all move the status forward
/// instead, so the affiliation history stays auditable.
/// </summary>
public class DoctorHospitalAffiliation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DoctorProfileId { get; set; }
    public DoctorProfile? DoctorProfile { get; set; }

    public Guid HospitalProfileId { get; set; }
    public HospitalProfile? HospitalProfile { get; set; }

    public AffiliationStatus Status { get; set; } = AffiliationStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    /// <summary>The hospital account user that approved, rejected or removed the doctor.</summary>
    public string? ReviewedByUserId { get; set; }

    /// <summary>Required whenever the status becomes Rejected.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// A doctor may flag at most one approved affiliation as their primary hospital;
    /// setting a new one clears the previous flag.
    /// </summary>
    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// True while the doctor and hospital are linked or a decision is outstanding. A doctor
    /// may not open a second request against the same hospital in this state.
    /// </summary>
    public bool BlocksNewRequest =>
        Status is AffiliationStatus.Pending or AffiliationStatus.Approved;
}
