using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

/// <summary>
/// A patient's digital insurance request tied to one appointment. Rows are never deleted -
/// every transition (start review, approve, reject, cancel) moves <see cref="Status"/>
/// forward and stamps the matching audit timestamp, so the full history stays queryable.
/// </summary>
public class InsuranceRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PatientProfileId { get; set; }
    public PatientProfile? PatientProfile { get; set; }

    public Guid HospitalProfileId { get; set; }
    public HospitalProfile? HospitalProfile { get; set; }

    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid InsuranceCompanyId { get; set; }
    public InsuranceCompany? InsuranceCompany { get; set; }

    public string MemberNumber { get; set; } = string.Empty;
    public string? PolicyNumber { get; set; }
    public string ServiceDescription { get; set; } = string.Empty;

    public decimal? RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }

    public string? PatientNotes { get; set; }
    public string? HospitalNotes { get; set; }

    public string? InsuranceCardImageUrl { get; set; }
    public string? SupportingDocumentUrl { get; set; }

    public InsuranceRequestStatus Status { get; set; } = InsuranceRequestStatus.Pending;

    public string? RejectionReason { get; set; }
    public string? ApprovalReferenceNumber { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// True for Pending, UnderReview and Approved - the statuses that block a second active
    /// request for the same appointment (see the duplicate-request rules). Not mapped;
    /// queries inline this condition so it stays translatable by EF Core.
    /// </summary>
    public bool BlocksNewRequest => Status is InsuranceRequestStatus.Pending
        or InsuranceRequestStatus.UnderReview
        or InsuranceRequestStatus.Approved;
}
