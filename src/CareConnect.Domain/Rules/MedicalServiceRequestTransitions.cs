using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Rules;

/// <summary>Single source of truth for medical-service-request status transitions.</summary>
public static class MedicalServiceRequestTransitions
{
    public static bool CanTransition(
        MedicalServiceRequestStatus current,
        MedicalServiceRequestStatus target) =>
        current switch
        {
            MedicalServiceRequestStatus.Pending =>
                target is MedicalServiceRequestStatus.Accepted
                    or MedicalServiceRequestStatus.Rejected
                    or MedicalServiceRequestStatus.CancelledByPatient,
            MedicalServiceRequestStatus.Accepted =>
                target is MedicalServiceRequestStatus.Completed
                    or MedicalServiceRequestStatus.CancelledByPatient
                    or MedicalServiceRequestStatus.CancelledByProvider,
            _ => false
        };

    public static bool IsFinal(MedicalServiceRequestStatus status) =>
        status is MedicalServiceRequestStatus.Rejected
            or MedicalServiceRequestStatus.CancelledByPatient
            or MedicalServiceRequestStatus.CancelledByProvider
            or MedicalServiceRequestStatus.Completed;
}
