namespace CareConnect.Domain.Entities;

/// <summary>
/// A block of time a doctor is not bookable at one hospital - a single absence, a partial
/// day, or a vacation span. Slot generation subtracts these; nothing here is recurring.
/// </summary>
public class DoctorUnavailablePeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DoctorProfileId { get; set; }
    public DoctorProfile? DoctorProfile { get; set; }

    public Guid HospitalProfileId { get; set; }
    public HospitalProfile? HospitalProfile { get; set; }

    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool Overlaps(DateTime rangeStart, DateTime rangeEnd) =>
        StartDateTime < rangeEnd && rangeStart < EndDateTime;
}
