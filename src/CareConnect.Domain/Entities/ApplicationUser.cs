using Microsoft.AspNetCore.Identity;

namespace CareConnect.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>A deactivated user keeps their data but can no longer sign in.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    // Exactly one of these is populated, depending on the role chosen at registration.
    public PatientProfile? PatientProfile { get; set; }
    public DoctorProfile? DoctorProfile { get; set; }
    public HospitalProfile? HospitalProfile { get; set; }
    public MedicalServiceProviderProfile? MedicalServiceProviderProfile { get; set; }
}
