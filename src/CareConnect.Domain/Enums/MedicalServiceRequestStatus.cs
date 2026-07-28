namespace CareConnect.Domain.Enums;

/// <summary>Lifecycle of a patient medical-service request. Values are intentionally stable.</summary>
public enum MedicalServiceRequestStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    CancelledByPatient = 4,
    CancelledByProvider = 5,
    Completed = 6
}
