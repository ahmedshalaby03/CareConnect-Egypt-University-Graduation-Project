using System.Net.Http.Json;
using CareConnect.Domain.Constants;

namespace CareConnect.Api.IntegrationTests;

// --------------------------------------------------------------- Payload shapes

public class AvailabilityPayload
{
    public Guid Id { get; set; }
    public Guid HospitalProfileId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string DayOfWeekName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SlotDurationMinutes { get; set; }
    public bool IsActive { get; set; }
}

public class UnavailablePeriodPayload
{
    public Guid Id { get; set; }
    public Guid HospitalProfileId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Reason { get; set; }
}

public class SlotPayload
{
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}

public class AvailableSlotsPayload
{
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public Guid HospitalProfileId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int SlotDurationMinutes { get; set; }
    public List<SlotPayload> Slots { get; set; } = [];
}

public class PatientAppointmentPayload
{
    public Guid AppointmentId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? PatientNotes { get; set; }
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public Guid HospitalProfileId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
}

public class DoctorAppointmentPayload
{
    public Guid AppointmentId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? PatientNotes { get; set; }
    public string? DoctorNotes { get; set; }
    public Guid PatientProfileId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhoneNumber { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class HospitalAppointmentPayload
{
    public Guid AppointmentId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
}

public class PatientDashboardStatsPayload
{
    public PatientAppointmentPayload? NextAppointment { get; set; }
    public int UpcomingCount { get; set; }
    public int PendingCount { get; set; }
}

public class DoctorDashboardStatsPayload
{
    public int TodayCount { get; set; }
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int CompletedThisMonthCount { get; set; }
}

public class HospitalDashboardStatsPayload
{
    public int TodayCount { get; set; }
    public int PendingCount { get; set; }
    public int ActiveApprovedDoctorsCount { get; set; }
}

/// <summary>A doctor and hospital with an Approved affiliation and matching specialty, ready to schedule.</summary>
public record ApprovedPair(
    HttpClient Doctor,
    HttpClient Hospital,
    Guid DoctorProfileId,
    Guid HospitalProfileId,
    Guid SpecialtyId);

public static class AppointmentTestSupportExtensions
{
    /// <summary>
    /// A completed doctor, a completed hospital offering the doctor's specialty, and an
    /// Approved affiliation between them - the baseline every scheduling test starts from.
    /// </summary>
    public static async Task<ApprovedPair> ApprovedPairAsync(
        this HealthcareScenario scenario,
        HttpClient adminClient,
        string prefix)
    {
        var specialty = await scenario.AnySpecialtyAsync(adminClient);

        var (doctorClient, _, doctorProfile) = await scenario.CompletedDoctorAsync(specialty.Id, $"{prefix}-doc");
        var (hospitalClient, _, hospitalProfile) =
            await scenario.CompletedHospitalAsync([specialty.Id], $"{prefix}-hosp");

        var request = await doctorClient.PostAsJsonAsync(
            "/api/doctor/hospital-requests", new { hospitalProfileId = hospitalProfile.Id });
        request.EnsureSuccessStatusCode();

        var requestPayload = (await request.ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        var approve = await hospitalClient.PatchAsync(
            $"/api/hospital/doctor-requests/{requestPayload.Id}/approve", null);
        approve.EnsureSuccessStatusCode();

        return new ApprovedPair(doctorClient, hospitalClient, doctorProfile.Id, hospitalProfile.Id, specialty.Id);
    }

    /// <summary>
    /// Adds a wide-open availability block covering all of tomorrow's matching weekday, so
    /// booking tests always have a guaranteed-future, guaranteed-open day to work with
    /// regardless of what time the test suite happens to run.
    /// </summary>
    public static async Task<(AvailabilityPayload Availability, DateOnly BookableDate)> AddTomorrowAvailabilityAsync(
        this ApprovedPair pair,
        int slotDurationMinutes = 30)
    {
        var bookableDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var response = await pair.Doctor.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            dayOfWeek = (int)bookableDate.DayOfWeek,
            startTime = "00:00",
            endTime = "23:50",
            slotDurationMinutes
        });

        response.EnsureSuccessStatusCode();
        var availability = (await response.ReadEnvelopeAsync<AvailabilityPayload>()).Data!;

        return (availability, bookableDate);
    }

    public static async Task<List<SlotPayload>> GetSlotsAsync(
        this ApprovedPair pair,
        HttpClient client,
        DateOnly date)
    {
        var response = await client.GetAsync(
            $"/api/doctors/{pair.DoctorProfileId}/available-slots" +
            $"?hospitalProfileId={pair.HospitalProfileId}&date={date:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();
        return (await response.ReadEnvelopeAsync<AvailableSlotsPayload>()).Data!.Slots;
    }

    /// <summary>
    /// Books one available slot on the given date. Fails the test if there is no slot at
    /// <paramref name="slotIndex"/>. The index matters when the same patient is booked
    /// against several pairs that share the same wide-open "tomorrow" window (see
    /// <see cref="AddTomorrowAvailabilityAsync"/>): slot 0 would be the same wall-clock
    /// time for all of them and collide with the patient's own overlap rule, so callers
    /// booking more than one appointment for one patient must pass distinct indices.
    /// </summary>
    public static async Task<PatientAppointmentPayload> BookFirstSlotAsync(
        this ApprovedPair pair,
        HttpClient patientClient,
        DateOnly date,
        string reason = "Routine consultation",
        int slotIndex = 0)
    {
        var slots = await pair.GetSlotsAsync(patientClient, date);
        Assert.True(slots.Count > slotIndex, $"Expected at least {slotIndex + 1} slot(s), found {slots.Count}.");

        var response = await patientClient.PostAsJsonAsync("/api/patient/appointments", new
        {
            doctorProfileId = pair.DoctorProfileId,
            hospitalProfileId = pair.HospitalProfileId,
            appointmentDate = date.ToString("yyyy-MM-dd"),
            startTime = slots[slotIndex].StartTime,
            reason,
            patientNotes = (string?)null
        });

        response.EnsureSuccessStatusCode();
        return (await response.ReadEnvelopeAsync<PatientAppointmentPayload>()).Data!;
    }
}
