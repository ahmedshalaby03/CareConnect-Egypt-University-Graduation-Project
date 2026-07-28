using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

/// <summary>
/// A patient's request for one provider catalog offering. Snapshot fields preserve the
/// commercial details that were visible when the request was submitted.
/// </summary>
public class MedicalServiceRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RequestNumber { get; set; } = string.Empty;

    public Guid PatientProfileId { get; set; }
    public PatientProfile? PatientProfile { get; set; }

    public Guid MedicalServiceProviderProfileId { get; set; }
    public MedicalServiceProviderProfile? MedicalServiceProviderProfile { get; set; }

    public Guid MedicalServiceOfferingId { get; set; }
    public MedicalServiceOffering? MedicalServiceOffering { get; set; }

    public MedicalServiceRequestStatus Status { get; set; } = MedicalServiceRequestStatus.Pending;
    public ServiceDeliveryMode DeliveryMode { get; set; }
    public DateOnly RequestedDate { get; set; }
    public TimeOnly PreferredStartTime { get; set; }

    /// <summary>
    /// Confirmed Egypt local wall-clock date/time, stored without a UTC suffix to match the
    /// existing DateOnly/TimeOnly appointment convention. Audit timestamps remain UTC.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    public string? PatientNotes { get; set; }
    public string? HomeVisitAddress { get; set; }
    public string? ProviderResponseNote { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }

    public string ServiceNameSnapshot { get; set; } = string.Empty;
    public string CategoryNameSnapshot { get; set; } = string.Empty;
    public decimal PriceSnapshot { get; set; }
    public int? DurationMinutesSnapshot { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>Optimistic concurrency token for competing provider actions.</summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<MedicalServiceRequestStatusHistory> StatusHistory { get; set; } =
        new List<MedicalServiceRequestStatusHistory>();
    public MedicalServiceProviderReview? Review { get; set; }
}
