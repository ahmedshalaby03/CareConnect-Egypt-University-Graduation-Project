using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

/// <summary>
/// A patient's request for blood units at one hospital, submitted for themselves or for a
/// named beneficiary. Rows are never deleted - every transition (approve, reject, fulfill,
/// cancel) moves <see cref="Status"/> forward and stamps the matching audit timestamp.
/// </summary>
public class BloodRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PatientProfileId { get; set; }
    public PatientProfile? PatientProfile { get; set; }

    public Guid HospitalProfileId { get; set; }
    public HospitalProfile? HospitalProfile { get; set; }

    public Guid BloodStockId { get; set; }
    public BloodStock? BloodStock { get; set; }

    public BloodGroup BloodGroup { get; set; }
    public int UnitsRequested { get; set; }

    public string BeneficiaryName { get; set; } = string.Empty;
    public int? BeneficiaryAge { get; set; }
    public string ContactPhoneNumber { get; set; } = string.Empty;
    public string? MedicalCondition { get; set; }
    public string? HospitalOrFacilityName { get; set; }
    public string? RequestNotes { get; set; }
    public string? HospitalNotes { get; set; }

    public BloodRequestUrgency Urgency { get; set; } = BloodRequestUrgency.Normal;
    public BloodRequestStatus Status { get; set; } = BloodRequestStatus.Pending;

    public string? RejectionReason { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// True for Pending and Approved - the statuses that block a second active duplicate
    /// request. Not mapped; queries inline this condition so it stays translatable by EF Core.
    /// </summary>
    public bool IsActiveForDuplicateCheck => Status is BloodRequestStatus.Pending or BloodRequestStatus.Approved;
}
