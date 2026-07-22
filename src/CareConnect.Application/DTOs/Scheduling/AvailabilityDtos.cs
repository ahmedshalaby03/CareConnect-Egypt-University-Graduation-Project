namespace CareConnect.Application.DTOs.Scheduling;

public class AvailabilityDto
{
    public Guid Id { get; init; }
    public Guid HospitalProfileId { get; init; }
    public string HospitalName { get; init; } = string.Empty;

    public DayOfWeek DayOfWeek { get; init; }
    public string DayOfWeekName { get; init; } = string.Empty;

    /// <summary>"HH:mm" format.</summary>
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;

    public int SlotDurationMinutes { get; init; }
    public bool IsActive { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class CreateAvailabilityRequest
{
    public Guid HospitalProfileId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>"HH:mm" or "HH:mm:ss".</summary>
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;

    public int SlotDurationMinutes { get; set; }
}

public class UpdateAvailabilityRequest
{
    public Guid HospitalProfileId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SlotDurationMinutes { get; set; }
}

public class AvailabilityQueryParameters
{
    public Guid? HospitalProfileId { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public bool? IsActive { get; set; }
}
