using System.Net;
using System.Net.Http.Json;
using CareConnect.Domain.Constants;

namespace CareConnect.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class UnavailablePeriodEndpointsTests
{
    private readonly CareConnectApiFactory _factory;
    private readonly HealthcareScenario _scenario;

    public UnavailablePeriodEndpointsTests(CareConnectApiFactory factory)
    {
        _factory = factory;
        _scenario = new HealthcareScenario(factory);
    }

    [Fact]
    public async Task Doctor_CanAddAFuturePeriodAndDeleteItAgain()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "unavail-add");

        var start = DateTime.UtcNow.AddDays(5);
        var end = start.AddHours(3);

        var response = await pair.Doctor.PostAsJsonAsync("/api/doctor/unavailable-periods", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            startDateTime = start,
            endDateTime = end,
            reason = "Conference"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.ReadEnvelopeAsync<UnavailablePeriodPayload>()).Data!;

        var delete = await pair.Doctor.DeleteAsync($"/api/doctor/unavailable-periods/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    [Fact]
    public async Task Doctor_CannotAddAPeriodThatOverlapsAPendingOrConfirmedAppointment()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "unavail-conflict");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "unavail-conflict-patient");
        var booked = await pair.BookFirstSlotAsync(patient, date);

        // The whole bookable day, which certainly overlaps the appointment just made.
        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = date.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc);

        var response = await pair.Doctor.PostAsJsonAsync("/api/doctor/unavailable-periods", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            startDateTime = dayStart,
            endDateTime = dayEnd,
            reason = "Trying to block a day that already has a booking"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var message = (await response.ReadEnvelopeAsync<object>()).Message;
        Assert.Contains("pending or confirmed appointment", message, StringComparison.OrdinalIgnoreCase);

        // The appointment itself was never touched.
        var stillPending = await pair.Doctor.GetAsync($"/api/doctor/appointments/{booked.AppointmentId}");
        Assert.Equal("Pending",
            (await stillPending.ReadEnvelopeAsync<DoctorAppointmentPayload>()).Data!.StatusName);
    }

    [Fact]
    public async Task UnavailablePeriodRemovesTheAffectedSlotsFromTheDay()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "unavail-removes-slots");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "unavail-slots-patient");

        var before = await pair.GetSlotsAsync(patient, date);
        Assert.NotEmpty(before);

        var period = await pair.Doctor.PostAsJsonAsync("/api/doctor/unavailable-periods", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            startDateTime = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            endDateTime = date.ToDateTime(new TimeOnly(23, 59), DateTimeKind.Utc),
            reason = "Full day off"
        });
        Assert.Equal(HttpStatusCode.Created, period.StatusCode);

        var after = await pair.GetSlotsAsync(patient, date);
        Assert.Empty(after);
    }

    [Fact]
    public async Task Doctor_CannotDeleteAPastPeriodOrAnotherDoctorsPeriod()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pairA = await _scenario.ApprovedPairAsync(admin, "unavail-owner-a");
        var pairB = await _scenario.ApprovedPairAsync(admin, "unavail-owner-b");

        var created = await pairA.Doctor.PostAsJsonAsync("/api/doctor/unavailable-periods", new
        {
            hospitalProfileId = pairA.HospitalProfileId,
            startDateTime = DateTime.UtcNow.AddDays(2),
            endDateTime = DateTime.UtcNow.AddDays(2).AddHours(1),
            reason = "Future"
        });

        var id = (await created.ReadEnvelopeAsync<UnavailablePeriodPayload>()).Data!.Id;

        var otherDoctorDelete = await pairB.Doctor.DeleteAsync($"/api/doctor/unavailable-periods/{id}");
        Assert.Equal(HttpStatusCode.NotFound, otherDoctorDelete.StatusCode);

        var stillThere = await (await pairA.Doctor.GetAsync("/api/doctor/unavailable-periods"))
            .ReadEnvelopeAsync<List<UnavailablePeriodPayload>>();
        Assert.Contains(stillThere.Data!, p => p.Id == id);
    }

    [Theory]
    [InlineData(AppRoles.Patient)]
    [InlineData(AppRoles.Hospital)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task UnavailablePeriodEndpoints_AreClosedToOtherRoles(string role)
    {
        var (client, _) = await _scenario.NewAccountAsync(role, $"unavail-403-{role.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("/api/doctor/unavailable-periods")).StatusCode);
    }

    [Fact]
    public async Task UnavailablePeriodEndpoints_Return401ForAnonymousCallers()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/doctor/unavailable-periods")).StatusCode);
    }
}
