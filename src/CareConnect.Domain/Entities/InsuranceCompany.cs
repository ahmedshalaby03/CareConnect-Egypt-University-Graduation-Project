namespace CareConnect.Domain.Entities;

/// <summary>
/// A third-party insurer patients can file a digital request against. Managed centrally by
/// the SuperAdmin so every patient and hospital works from the same controlled list.
///
/// Never deleted. Deactivating one hides it from the patient's request form while every
/// request that already references it keeps working.
/// </summary>
public class InsuranceCompany
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string? ArabicName { get; set; }
    public string? Description { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<InsuranceRequest> InsuranceRequests { get; set; } = new List<InsuranceRequest>();
}
