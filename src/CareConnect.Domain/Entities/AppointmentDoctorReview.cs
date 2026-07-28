using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

public class AppointmentDoctorReview : IVerifiedReview
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public Guid PatientProfileId { get; set; }
    public PatientProfile? PatientProfile { get; set; }
    public Guid DoctorProfileId { get; set; }
    public DoctorProfile? DoctorProfile { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public ReviewModerationStatus ModerationStatus { get; set; } = ReviewModerationStatus.Visible;
    public string? ModerationReason { get; set; }
    public string? ModeratedByApplicationUserId { get; set; }
    public ApplicationUser? ModeratedByApplicationUser { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
