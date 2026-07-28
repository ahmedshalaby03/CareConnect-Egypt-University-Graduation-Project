using CareConnect.Domain.Enums;

namespace CareConnect.Domain.Entities;

public class MedicalServiceProviderProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string? BusinessName { get; set; }
    public MedicalServiceProviderType? ProviderType { get; set; }
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MedicalServiceOffering> ServiceOfferings { get; set; } =
        new List<MedicalServiceOffering>();
    public ICollection<MedicalServiceProviderWorkingHour> WorkingHours { get; set; } =
        new List<MedicalServiceProviderWorkingHour>();
    public ICollection<MedicalServiceRequest> MedicalServiceRequests { get; set; } =
        new List<MedicalServiceRequest>();
}
