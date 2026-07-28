namespace CareConnect.Domain.Entities;

public class MedicalServiceCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MedicalServiceOffering> ServiceOfferings { get; set; } =
        new List<MedicalServiceOffering>();
}
