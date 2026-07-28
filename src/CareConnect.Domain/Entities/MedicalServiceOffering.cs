namespace CareConnect.Domain.Entities;

public class MedicalServiceOffering
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MedicalServiceProviderProfileId { get; set; }
    public MedicalServiceProviderProfile? MedicalServiceProviderProfile { get; set; }

    public Guid MedicalServiceCategoryId { get; set; }
    public MedicalServiceCategory? MedicalServiceCategory { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? PreparationInstructions { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
