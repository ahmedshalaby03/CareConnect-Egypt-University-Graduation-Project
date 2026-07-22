namespace CareConnect.Domain.Entities;

public class MedicalServiceProviderProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string? ProviderName { get; set; }

    /// <summary>Free text for now (Pharmacy, Laboratory, Radiology Center, ...).</summary>
    public string? ServiceType { get; set; }

    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? Description { get; set; }
}
