using CareConnect.Application.DTOs.Notifications;
using CareConnect.Domain.Enums;

namespace CareConnect.Infrastructure.Services;

internal static class WorkflowNotificationFactory
{
    public static CreateNotificationCommand AppointmentBooked(Guid id, string doctorUserId) =>
        Create(doctorUserId, NotificationType.ActionRequired, NotificationCategory.Appointment,
            "New appointment request", "A patient submitted a new appointment request.",
            NotificationRelatedEntityType.Appointment, id,
            $"/dashboard/doctor/appointments/{id}",
            $"appointment:{id}:created:doctor:{doctorUserId}");

    public static CreateNotificationCommand AppointmentPatientUpdate(
        Guid id, string patientUserId, string transition, string title,
        string message, NotificationType type) =>
        Create(patientUserId, type, NotificationCategory.Appointment, title, message,
            NotificationRelatedEntityType.Appointment, id,
            $"/dashboard/patient/appointments/{id}",
            $"appointment:{id}:{transition}:patient:{patientUserId}");

    public static CreateNotificationCommand AppointmentCancelledForOwner(
        Guid id, string recipientUserId, string recipientRole) =>
        Create(recipientUserId, NotificationType.Warning, NotificationCategory.Appointment,
            "Appointment cancelled by patient",
            "A patient cancelled an appointment.",
            NotificationRelatedEntityType.Appointment, id,
            recipientRole == "doctor"
                ? $"/dashboard/doctor/appointments/{id}"
                : "/dashboard/hospital/appointments",
            $"appointment:{id}:cancelled-by-patient:{recipientRole}:{recipientUserId}");

    public static CreateNotificationCommand InsuranceSubmitted(Guid id, string hospitalUserId) =>
        Create(hospitalUserId, NotificationType.ActionRequired, NotificationCategory.Insurance,
            "New insurance request", "A patient submitted a new insurance request.",
            NotificationRelatedEntityType.InsuranceRequest, id,
            $"/dashboard/hospital/insurance-requests/{id}",
            $"insurance-request:{id}:submitted:hospital:{hospitalUserId}");

    public static CreateNotificationCommand InsurancePatientUpdate(
        Guid id, string patientUserId, string transition, string title, string message,
        NotificationType type) =>
        Create(patientUserId, type, NotificationCategory.Insurance, title, message,
            NotificationRelatedEntityType.InsuranceRequest, id,
            $"/dashboard/patient/insurance-requests/{id}",
            $"insurance-request:{id}:{transition}:patient:{patientUserId}");

    public static CreateNotificationCommand BloodSubmitted(Guid id, string hospitalUserId) =>
        Create(hospitalUserId, NotificationType.ActionRequired, NotificationCategory.BloodBank,
            "New blood request", "A patient submitted a new blood request.",
            NotificationRelatedEntityType.BloodRequest, id,
            $"/dashboard/hospital/blood-requests/{id}",
            $"blood-request:{id}:submitted:hospital:{hospitalUserId}");

    public static CreateNotificationCommand BloodPatientUpdate(
        Guid id, string patientUserId, string transition, string title, string message,
        NotificationType type) =>
        Create(patientUserId, type, NotificationCategory.BloodBank, title, message,
            NotificationRelatedEntityType.BloodRequest, id,
            $"/dashboard/patient/blood-requests/{id}",
            $"blood-request:{id}:{transition}:patient:{patientUserId}");

    public static CreateNotificationCommand BloodCancelledByPatient(Guid id, string hospitalUserId) =>
        Create(hospitalUserId, NotificationType.Warning, NotificationCategory.BloodBank,
            "Blood request cancelled by patient",
            "A patient cancelled a pending blood request.",
            NotificationRelatedEntityType.BloodRequest, id,
            $"/dashboard/hospital/blood-requests/{id}",
            $"blood-request:{id}:cancelled-by-patient:hospital:{hospitalUserId}");

    public static CreateNotificationCommand MedicalServiceSubmitted(Guid id, string providerUserId) =>
        Create(providerUserId, NotificationType.ActionRequired, NotificationCategory.MedicalService,
            "New medical service request", "A patient submitted a new medical service request.",
            NotificationRelatedEntityType.MedicalServiceRequest, id,
            $"/dashboard/service-provider/requests/{id}",
            $"medical-service-request:{id}:submitted:provider:{providerUserId}");

    public static CreateNotificationCommand MedicalServicePatientUpdate(
        Guid id, string patientUserId, string transition, string title, string message,
        NotificationType type) =>
        Create(patientUserId, type, NotificationCategory.MedicalService, title, message,
            NotificationRelatedEntityType.MedicalServiceRequest, id,
            $"/dashboard/patient/service-requests/{id}",
            $"medical-service-request:{id}:{transition}:patient:{patientUserId}");

    public static CreateNotificationCommand MedicalServiceCancelledByPatient(
        Guid id, string providerUserId) =>
        Create(providerUserId, NotificationType.Warning, NotificationCategory.MedicalService,
            "Service request cancelled by patient",
            "A patient cancelled a medical service request.",
            NotificationRelatedEntityType.MedicalServiceRequest, id,
            $"/dashboard/service-provider/requests/{id}",
            $"medical-service-request:{id}:cancelled-by-patient:provider:{providerUserId}");

    public static CreateNotificationCommand AffiliationSubmitted(Guid id, string hospitalUserId) =>
        Create(hospitalUserId, NotificationType.ActionRequired, NotificationCategory.HospitalAffiliation,
            "New doctor affiliation request",
            "A doctor submitted a new affiliation request.",
            NotificationRelatedEntityType.DoctorHospitalAffiliation, id,
            "/dashboard/hospital/doctor-requests",
            $"affiliation:{id}:submitted:hospital:{hospitalUserId}");

    public static CreateNotificationCommand AffiliationDoctorUpdate(
        Guid id, string doctorUserId, string transition, string title, NotificationType type) =>
        Create(doctorUserId, type, NotificationCategory.HospitalAffiliation,
            title, $"Your affiliation request was {transition}.",
            NotificationRelatedEntityType.DoctorHospitalAffiliation, id,
            "/dashboard/doctor/hospital-requests",
            $"affiliation:{id}:{transition}:doctor:{doctorUserId}");

    public static CreateNotificationCommand NewReview(
        Guid reviewId, string ownerUserId, string ownerReviewRoute, string reviewKind) =>
        Create(ownerUserId, NotificationType.Information, NotificationCategory.Review,
            "New verified review",
            $"A patient submitted a new verified {reviewKind} review.",
            NotificationRelatedEntityType.Review, reviewId, ownerReviewRoute,
            $"review:{reviewKind}:{reviewId}:created:owner:{ownerUserId}");

    public static CreateNotificationCommand ReviewModerated(
        Guid reviewId, string patientUserId, string transition, string title,
        NotificationType type) =>
        Create(patientUserId, type, NotificationCategory.Review, title,
            transition == "hidden"
                ? "One of your verified reviews was hidden by moderation."
                : "One of your verified reviews was restored and is visible again.",
            NotificationRelatedEntityType.Review, reviewId,
            "/dashboard/patient/reviews",
            $"review:{reviewId}:{transition}:patient:{patientUserId}");

    private static CreateNotificationCommand Create(
        string recipientUserId,
        NotificationType type,
        NotificationCategory category,
        string title,
        string message,
        NotificationRelatedEntityType entityType,
        Guid entityId,
        string route,
        string key) =>
        new()
        {
            RecipientApplicationUserId = recipientUserId,
            Type = type,
            Category = category,
            Title = title,
            Message = message,
            RelatedEntityType = entityType,
            RelatedEntityId = entityId,
            ActionRoute = route,
            DeduplicationKey = key
        };
}
