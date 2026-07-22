namespace CareConnect.Application.DTOs.Scheduling;

/// <summary>One bookable slot. "HH:mm:ss" strings, matching the appointment DTOs.</summary>
public class SlotDto
{
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
}

public class AvailableSlotsResponse
{
    public Guid DoctorProfileId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public Guid HospitalProfileId { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public int SlotDurationMinutes { get; init; }
    public IReadOnlyList<SlotDto> Slots { get; init; } = [];
}
