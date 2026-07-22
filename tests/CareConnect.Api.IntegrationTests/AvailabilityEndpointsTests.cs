using System.Net;
using System.Net.Http.Json;
using CareConnect.Domain.Constants;

namespace CareConnect.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class AvailabilityEndpointsTests
{
    private readonly CareConnectApiFactory _factory;
    private readonly HealthcareScenario _scenario;

    public AvailabilityEndpointsTests(CareConnectApiFactory factory)
    {
        _factory = factory;
        _scenario = new HealthcareScenario(factory);
    }

    [Fact]
    public async Task Doctor_CanAddWeeklyAvailabilityAtAnApprovedHospital()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "avail-add");

        var response = await pair.Doctor.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            dayOfWeek = 0,
            startTime = "09:00",
            endTime = "14:00",
            slotDurationMinutes = 30
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = (await response.ReadEnvelopeAsync<AvailabilityPayload>()).Data!;
        Assert.Equal("09:00", created.StartTime);
        Assert.Equal("14:00", created.EndTime);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Doctor_CannotAddAvailabilityWithoutAnApprovedAffiliation()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);

        var (doctor, _, _) = await _scenario.CompletedDoctorAsync(specialty.Id, "avail-noaffil-doc");
        var (_, _, hospital) = await _scenario.CompletedHospitalAsync([specialty.Id], "avail-noaffil-hosp");

        var response = await doctor.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = hospital.Id,
            dayOfWeek = 1,
            startTime = "09:00",
            endTime = "12:00",
            slotDurationMinutes = 30
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("approved affiliation",
            (await response.ReadEnvelopeAsync<object>()).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Doctor_RejectsStartTimeNotBeforeEndTimeAndBadDuration()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "avail-badinput");

        var badOrder = await pair.Doctor.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            dayOfWeek = 2,
            startTime = "14:00",
            endTime = "09:00",
            slotDurationMinutes = 30
        });
        Assert.Equal(HttpStatusCode.BadRequest, badOrder.StatusCode);

        var badDuration = await pair.Doctor.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            dayOfWeek = 2,
            startTime = "09:00",
            endTime = "14:00",
            slotDurationMinutes = 200
        });
        Assert.Equal(HttpStatusCode.BadRequest, badDuration.StatusCode);
    }

    [Fact]
    public async Task Doctor_OverlappingActiveScheduleIsRejectedButDifferentHospitalIsFine()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);

        var (doctorClient, _, doctorProfile) = await _scenario.CompletedDoctorAsync(specialty.Id, "avail-overlap-doc");
        var (hospitalAClient, _, hospitalA) = await _scenario.CompletedHospitalAsync([specialty.Id], "avail-overlap-a");
        var (hospitalBClient, _, hospitalB) = await _scenario.CompletedHospitalAsync([specialty.Id], "avail-overlap-b");

        foreach (var (hospitalClient, hospitalId) in new[] { (hospitalAClient, hospitalA.Id), (hospitalBClient, hospitalB.Id) })
        {
            var request = await doctorClient.PostAsJsonAsync(
                "/api/doctor/hospital-requests", new { hospitalProfileId = hospitalId });
            var requestId = (await request.ReadEnvelopeAsync<DoctorRequestPayload>()).Data!.Id;
            await hospitalClient.PatchAsync($"/api/hospital/doctor-requests/{requestId}/approve", null);
        }

        var first = await doctorClient.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = hospitalA.Id,
            dayOfWeek = 3,
            startTime = "09:00",
            endTime = "14:00",
            slotDurationMinutes = 30
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Overlaps 09:00-14:00 at the same hospital on the same day.
        var overlapping = await doctorClient.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = hospitalA.Id,
            dayOfWeek = 3,
            startTime = "13:00",
            endTime = "16:00",
            slotDurationMinutes = 30
        });
        Assert.Equal(HttpStatusCode.Conflict, overlapping.StatusCode);

        // Same day and time, but a different hospital - a doctor may have different
        // schedules at different hospitals.
        var otherHospital = await doctorClient.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = hospitalB.Id,
            dayOfWeek = 3,
            startTime = "09:00",
            endTime = "14:00",
            slotDurationMinutes = 30
        });
        Assert.Equal(HttpStatusCode.Created, otherHospital.StatusCode);

        Assert.NotEqual(default, doctorProfile.Id);
    }

    [Fact]
    public async Task Doctor_UpdateRevalidatesOverlapAndAffiliation()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "avail-update");

        var morning = await pair.Doctor.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            dayOfWeek = 4,
            startTime = "08:00",
            endTime = "11:00",
            slotDurationMinutes = 20
        });
        var morningId = (await morning.ReadEnvelopeAsync<AvailabilityPayload>()).Data!.Id;

        var afternoon = await pair.Doctor.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            dayOfWeek = 4,
            startTime = "14:00",
            endTime = "17:00",
            slotDurationMinutes = 20
        });
        Assert.Equal(HttpStatusCode.Created, afternoon.StatusCode);

        // Stretching the morning block into the afternoon block must be rejected.
        var stretched = await pair.Doctor.PutAsJsonAsync($"/api/doctor/availability/{morningId}", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            dayOfWeek = 4,
            startTime = "08:00",
            endTime = "15:00",
            slotDurationMinutes = 20
        });
        Assert.Equal(HttpStatusCode.Conflict, stretched.StatusCode);

        // A harmless edit still succeeds.
        var resized = await pair.Doctor.PutAsJsonAsync($"/api/doctor/availability/{morningId}", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            dayOfWeek = 4,
            startTime = "08:00",
            endTime = "10:00",
            slotDurationMinutes = 20
        });
        Assert.Equal(HttpStatusCode.OK, resized.StatusCode);
    }

    [Fact]
    public async Task Doctor_CanToggleStatusWithoutAffectingExistingAppointments()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "avail-toggle");
        var (availability, date) = await pair.AddTomorrowAvailabilityAsync();

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "avail-toggle-patient");
        var booked = await pair.BookFirstSlotAsync(patient, date);

        var deactivate = await pair.Doctor.PatchAsync(
            $"/api/doctor/availability/{availability.Id}/toggle-status", null);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        Assert.False((await deactivate.ReadEnvelopeAsync<AvailabilityPayload>()).Data!.IsActive);

        // The existing booking is untouched by deactivating the schedule it came from.
        var stillThere = await pair.Doctor.GetAsync($"/api/doctor/appointments/{booked.AppointmentId}");
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
        Assert.Equal("Pending",
            (await stillThere.ReadEnvelopeAsync<DoctorAppointmentPayload>()).Data!.StatusName);

        // Slots disappear while inactive.
        var slotsWhileInactive = await pair.GetSlotsAsync(patient, date);
        Assert.DoesNotContain(slotsWhileInactive, s => s.StartTime == booked.StartTime);

        var reactivate = await pair.Doctor.PatchAsync(
            $"/api/doctor/availability/{availability.Id}/toggle-status", null);
        Assert.True((await reactivate.ReadEnvelopeAsync<AvailabilityPayload>()).Data!.IsActive);
    }

    [Fact]
    public async Task Doctor_ListSupportsHospitalDayAndActiveFilters()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "avail-filters");

        await pair.Doctor.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            dayOfWeek = 5,
            startTime = "09:00",
            endTime = "12:00",
            slotDurationMinutes = 30
        });

        var byHospital = await (await pair.Doctor.GetAsync(
                $"/api/doctor/availability?hospitalProfileId={pair.HospitalProfileId}"))
            .ReadEnvelopeAsync<List<AvailabilityPayload>>();
        Assert.NotEmpty(byHospital.Data!);

        var byDay = await (await pair.Doctor.GetAsync("/api/doctor/availability?dayOfWeek=5"))
            .ReadEnvelopeAsync<List<AvailabilityPayload>>();
        Assert.Contains(byDay.Data!, a => a.DayOfWeek == 5);

        var activeOnly = await (await pair.Doctor.GetAsync("/api/doctor/availability?isActive=false"))
            .ReadEnvelopeAsync<List<AvailabilityPayload>>();
        Assert.DoesNotContain(activeOnly.Data!, a => a.DayOfWeek == 5);
    }

    [Fact]
    public async Task Doctor_CannotManageAnotherDoctorsAvailability()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pairA = await _scenario.ApprovedPairAsync(admin, "avail-owner-a");
        var pairB = await _scenario.ApprovedPairAsync(admin, "avail-owner-b");

        var created = await pairA.Doctor.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = pairA.HospitalProfileId,
            dayOfWeek = 6,
            startTime = "09:00",
            endTime = "12:00",
            slotDurationMinutes = 30
        });

        var availabilityId = (await created.ReadEnvelopeAsync<AvailabilityPayload>()).Data!.Id;

        var updateAttempt = await pairB.Doctor.PutAsJsonAsync($"/api/doctor/availability/{availabilityId}", new
        {
            hospitalProfileId = pairB.HospitalProfileId,
            dayOfWeek = 6,
            startTime = "09:00",
            endTime = "12:00",
            slotDurationMinutes = 30
        });
        Assert.Equal(HttpStatusCode.NotFound, updateAttempt.StatusCode);

        var toggleAttempt = await pairB.Doctor.PatchAsync(
            $"/api/doctor/availability/{availabilityId}/toggle-status", null);
        Assert.Equal(HttpStatusCode.NotFound, toggleAttempt.StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.Patient)]
    [InlineData(AppRoles.Hospital)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task AvailabilityEndpoints_AreClosedToOtherRoles(string role)
    {
        var (client, _) = await _scenario.NewAccountAsync(role, $"avail-403-{role.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/doctor/availability")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("/api/doctor/availability", new { })).StatusCode);
    }

    [Fact]
    public async Task AvailabilityEndpoints_Return401ForAnonymousCallers()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/doctor/availability")).StatusCode);
    }
}
