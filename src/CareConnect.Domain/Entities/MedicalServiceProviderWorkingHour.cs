namespace CareConnect.Domain.Entities;

public class MedicalServiceProviderWorkingHour
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MedicalServiceProviderProfileId { get; set; }
    public MedicalServiceProviderProfile? MedicalServiceProviderProfile { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public bool IsClosed { get; set; }
}
