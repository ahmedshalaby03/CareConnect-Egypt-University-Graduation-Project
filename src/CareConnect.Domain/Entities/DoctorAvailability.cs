namespace CareConnect.Domain.Entities;

/// <summary>
/// A recurring weekly time block a doctor works at one hospital. Slot times for any given
/// date are generated from this record, never stored - see <see cref="GenerateSlotStarts"/>.
/// </summary>
public class DoctorAvailability
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DoctorProfileId { get; set; }
    public DoctorProfile? DoctorProfile { get; set; }

    public Guid HospitalProfileId { get; set; }
    public HospitalProfile? HospitalProfile { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public int SlotDurationMinutes { get; set; }

    /// <summary>
    /// Deactivating hides this block from slot generation without touching appointments
    /// already booked against it - see business rule 9.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// The slot start times this block produces, e.g. 09:00-14:00 in 30-minute steps yields
    /// 09:00, 09:30, ... 13:30. The caller filters these against unavailable periods,
    /// existing appointments and "already in the past" separately.
    /// </summary>
    public IEnumerable<TimeOnly> GenerateSlotStarts()
    {
        // Minutes-since-midnight arithmetic on purpose: TimeOnly.Add wraps past 24:00 and a
        // wrapped value can compare as "earlier", which would corrupt the loop condition
        // for a block that runs close to midnight.
        var startMinutes = StartTime.Hour * 60 + StartTime.Minute;
        var endMinutes = EndTime.Hour * 60 + EndTime.Minute;

        for (var cursor = startMinutes; cursor + SlotDurationMinutes <= endMinutes; cursor += SlotDurationMinutes)
        {
            yield return new TimeOnly(cursor / 60, cursor % 60);
        }
    }
}
