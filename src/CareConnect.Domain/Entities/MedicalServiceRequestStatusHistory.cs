using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

/// <summary>Immutable audit entry for one medical-service-request status change.</summary>
public class MedicalServiceRequestStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MedicalServiceRequestId { get; set; }
    public MedicalServiceRequest? MedicalServiceRequest { get; set; }

    public MedicalServiceRequestStatus? PreviousStatus { get; set; }
    public MedicalServiceRequestStatus NewStatus { get; set; }

    public string? ChangedByApplicationUserId { get; set; }
    public ApplicationUser? ChangedByApplicationUser { get; set; }

    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
