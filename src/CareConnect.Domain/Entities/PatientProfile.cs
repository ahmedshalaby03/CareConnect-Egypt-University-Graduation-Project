namespace CareConnect.Domain.Entities;

public class PatientProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<InsuranceRequest> InsuranceRequests { get; set; } = new List<InsuranceRequest>();
    public ICollection<BloodRequest> BloodRequests { get; set; } = new List<BloodRequest>();
}
