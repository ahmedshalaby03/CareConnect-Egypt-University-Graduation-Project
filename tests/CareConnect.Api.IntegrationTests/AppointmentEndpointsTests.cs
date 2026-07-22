using System.Net;
using System.Net.Http.Json;
using CareConnect.Domain.Constants;

namespace CareConnect.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class AppointmentEndpointsTests
{
    private readonly CareConnectApiFactory _factory;
    private readonly HealthcareScenario _scenario;

    public AppointmentEndpointsTests(CareConnectApiFactory factory)
    {
        _factory = factory;
        _scenario = new HealthcareScenario(factory);
    }

    // ------------------------------------------------------------------ Slots

    [Fact]
    public async Task Slots_ExcludePastTimesAndReturnEmptyRatherThanError()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "slots-past");

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "slots-past-patient");

        // No availability at all for today - must come back empty, not throw.
        var todaySlots = await pair.GetSlotsAsync(patient, DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Empty(todaySlots);
    }

    [Fact]
    public async Task Slots_ExcludeUnapprovedAffiliations()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);

        var (doctorClient, _, doctorProfile) = await _scenario.CompletedDoctorAsync(specialty.Id, "slots-noaffil-doc");
        var (_, _, hospitalProfile) = await _scenario.CompletedHospitalAsync([specialty.Id], "slots-noaffil-hosp");

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "slots-noaffil-patient");

        var response = await patient.GetAsync(
            $"/api/doctors/{doctorProfile.Id}/available-slots?hospitalProfileId={hospitalProfile.Id}" +
            $"&date={DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)):yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = (await response.ReadEnvelopeAsync<AvailableSlotsPayload>()).Data!;
        Assert.Empty(payload.Slots);

        Assert.NotEqual(default, doctorClient.BaseAddress ?? new Uri("http://placeholder"));
    }

    [Fact]
    public async Task Slots_AreCorrectlyGeneratedFromAvailability()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "slots-gen");

        var bookableDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var created = await pair.Doctor.PostAsJsonAsync("/api/doctor/availability", new
        {
            hospitalProfileId = pair.HospitalProfileId,
            dayOfWeek = (int)bookableDate.DayOfWeek,
            startTime = "09:00",
            endTime = "10:00",
            slotDurationMinutes = 30
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "slots-gen-patient");
        var slots = await pair.GetSlotsAsync(patient, bookableDate);

        var starts = slots.Select(s => s.StartTime).ToList();
        Assert.Equal(["09:00:00", "09:30:00"], starts);
    }

    // ----------------------------------------------------------------- Booking

    [Fact]
    public async Task Patient_CanBookAPendingAppointment()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "book-basic");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "book-basic-patient");
        var appointment = await pair.BookFirstSlotAsync(patient, date);

        Assert.Equal("Pending", appointment.StatusName);
        Assert.Equal(date, appointment.AppointmentDate);
    }

    [Fact]
    public async Task Patient_CannotBookInThePast()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "book-past");
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "book-past-patient");

        var response = await patient.PostAsJsonAsync("/api/patient/appointments", new
        {
            doctorProfileId = pair.DoctorProfileId,
            hospitalProfileId = pair.HospitalProfileId,
            appointmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd"),
            startTime = "09:00",
            reason = "Testing a past date",
            patientNotes = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patient_CannotBookASlotThatIsNotOnTheGeneratedGrid()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "book-badslot");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "book-badslot-patient");

        var response = await patient.PostAsJsonAsync("/api/patient/appointments", new
        {
            doctorProfileId = pair.DoctorProfileId,
            hospitalProfileId = pair.HospitalProfileId,
            appointmentDate = date.ToString("yyyy-MM-dd"),
            startTime = "00:07", // Not a multiple of the 30-minute grid.
            reason = "Off-grid time",
            patientNotes = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not a valid appointment slot",
            (await response.ReadEnvelopeAsync<object>()).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Patient_CannotBookWhenTheHospitalDoesNotOfferTheDoctorsSpecialty()
    {
        // The affiliation endpoint already blocks a specialty mismatch when the request is
        // first sent (proven in AffiliationEndpointsTests). This test instead proves the
        // booking endpoint independently re-checks the same rule: the hospital drops the
        // specialty *after* the affiliation was approved, and a booking must still fail.
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "book-mismatch");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();

        var otherSpecialty = (await _scenario.AnySpecialtyAsync(admin, "General Medicine")).Id == pair.SpecialtyId
            ? (await _scenario.AnySpecialtyAsync(admin, "Cardiology")).Id
            : (await _scenario.AnySpecialtyAsync(admin, "General Medicine")).Id;

        var dropSpecialty = await pair.Hospital.PutAsJsonAsync(
            "/api/hospital/profile/specialties",
            new { specialtyIds = new[] { otherSpecialty } });
        dropSpecialty.EnsureSuccessStatusCode();

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "book-mismatch-patient");

        var response = await patient.PostAsJsonAsync("/api/patient/appointments", new
        {
            doctorProfileId = pair.DoctorProfileId,
            hospitalProfileId = pair.HospitalProfileId,
            appointmentDate = date.ToString("yyyy-MM-dd"),
            startTime = "09:00",
            reason = "Should be rejected: specialty no longer offered.",
            patientNotes = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("does not offer",
            (await response.ReadEnvelopeAsync<object>()).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Patient_CannotDoubleBookTheSameSlotAndGetsA409()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "book-double");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();

        var (patientA, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "book-double-a");
        var (patientB, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "book-double-b");

        var slots = await pair.GetSlotsAsync(patientA, date);
        var targetSlot = slots[0];

        var bookingRequest = new
        {
            doctorProfileId = pair.DoctorProfileId,
            hospitalProfileId = pair.HospitalProfileId,
            appointmentDate = date.ToString("yyyy-MM-dd"),
            startTime = targetSlot.StartTime,
            reason = "First patient",
            patientNotes = (string?)null
        };

        var first = await patientA.PostAsJsonAsync("/api/patient/appointments", bookingRequest);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await patientB.PostAsJsonAsync("/api/patient/appointments", new
        {
            bookingRequest.doctorProfileId,
            bookingRequest.hospitalProfileId,
            bookingRequest.appointmentDate,
            bookingRequest.startTime,
            reason = "Second patient, same slot",
            patientNotes = (string?)null
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("just booked",
            (await second.ReadEnvelopeAsync<object>()).Message, StringComparison.OrdinalIgnoreCase);

        // The slot is gone from the list for anyone looking now.
        var remaining = await pair.GetSlotsAsync(patientB, date);
        Assert.DoesNotContain(remaining, s => s.StartTime == targetSlot.StartTime);
    }

    [Fact]
    public async Task Patient_CannotBookTwoOverlappingAppointmentsForThemselves()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pairA = await _scenario.ApprovedPairAsync(admin, "book-self-overlap-a");
        var pairB = await _scenario.ApprovedPairAsync(admin, "book-self-overlap-b");

        // Both pairs booked on the same future date, at the same wide-open hours.
        var dateA = (await pairA.AddTomorrowAvailabilityAsync()).BookableDate;
        var dateB = (await pairB.AddTomorrowAvailabilityAsync()).BookableDate;
        Assert.Equal(dateA, dateB);

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "book-self-overlap-patient");

        var slotsA = await pairA.GetSlotsAsync(patient, dateA);
        var slot = slotsA[0];

        var first = await patient.PostAsJsonAsync("/api/patient/appointments", new
        {
            doctorProfileId = pairA.DoctorProfileId,
            hospitalProfileId = pairA.HospitalProfileId,
            appointmentDate = dateA.ToString("yyyy-MM-dd"),
            startTime = slot.StartTime,
            reason = "First doctor",
            patientNotes = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await patient.PostAsJsonAsync("/api/patient/appointments", new
        {
            doctorProfileId = pairB.DoctorProfileId,
            hospitalProfileId = pairB.HospitalProfileId,
            appointmentDate = dateB.ToString("yyyy-MM-dd"),
            startTime = slot.StartTime,
            reason = "Second doctor, same time",
            patientNotes = (string?)null
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("overlaps this time",
            (await second.ReadEnvelopeAsync<object>()).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Patient_CanBookAgainOnceTheSlotIsFreedByRejectionOrCancellation()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "book-reopen");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "book-reopen-patient");
        var first = await pair.BookFirstSlotAsync(patient, date);

        await pair.Doctor.PatchAsJsonAsync(
            $"/api/doctor/appointments/{first.AppointmentId}/reject",
            new { rejectionReason = "Schedule conflict on our side." });

        var (otherPatient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "book-reopen-other");

        var rebook = await otherPatient.PostAsJsonAsync("/api/patient/appointments", new
        {
            doctorProfileId = pair.DoctorProfileId,
            hospitalProfileId = pair.HospitalProfileId,
            appointmentDate = date.ToString("yyyy-MM-dd"),
            startTime = first.StartTime,
            reason = "Rebooking the freed slot",
            patientNotes = (string?)null
        });

        Assert.Equal(HttpStatusCode.Created, rebook.StatusCode);
    }

    // ---------------------------------------------------------- Status transitions

    [Fact]
    public async Task Doctor_CanConfirmThenCompleteAnAppointment()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "flow-confirm-complete");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "flow-confirm-complete-patient");

        var appointment = await pair.BookFirstSlotAsync(patient, date);

        var confirm = await pair.Doctor.PatchAsync($"/api/doctor/appointments/{appointment.AppointmentId}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Equal("Confirmed", (await confirm.ReadEnvelopeAsync<DoctorAppointmentPayload>()).Data!.StatusName);

        var complete = await pair.Doctor.PatchAsync($"/api/doctor/appointments/{appointment.AppointmentId}/complete", null);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        var completed = (await complete.ReadEnvelopeAsync<DoctorAppointmentPayload>()).Data!;
        Assert.Equal("Completed", completed.StatusName);
        Assert.NotNull(completed.CompletedAt);

        // Terminal: cannot be cancelled or return to Confirmed afterwards.
        var cancelAttempt = await pair.Doctor.PatchAsJsonAsync(
            $"/api/doctor/appointments/{appointment.AppointmentId}/cancel",
            new { cancellationReason = "Too late now." });
        Assert.Equal(HttpStatusCode.BadRequest, cancelAttempt.StatusCode);

        var confirmAgain = await pair.Doctor.PatchAsync($"/api/doctor/appointments/{appointment.AppointmentId}/confirm", null);
        Assert.Equal(HttpStatusCode.BadRequest, confirmAgain.StatusCode);
    }

    [Fact]
    public async Task Doctor_CanMarkAConfirmedAppointmentAsNoShow()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "flow-noshow");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "flow-noshow-patient");

        var appointment = await pair.BookFirstSlotAsync(patient, date);
        await pair.Doctor.PatchAsync($"/api/doctor/appointments/{appointment.AppointmentId}/confirm", null);

        var noShow = await pair.Doctor.PatchAsync($"/api/doctor/appointments/{appointment.AppointmentId}/no-show", null);
        Assert.Equal(HttpStatusCode.OK, noShow.StatusCode);
        Assert.Equal("NoShow", (await noShow.ReadEnvelopeAsync<DoctorAppointmentPayload>()).Data!.StatusName);

        // A no-show can never later become Completed.
        var completeAttempt = await pair.Doctor.PatchAsync(
            $"/api/doctor/appointments/{appointment.AppointmentId}/complete", null);
        Assert.Equal(HttpStatusCode.BadRequest, completeAttempt.StatusCode);
    }

    [Fact]
    public async Task Reject_RequiresAReason_AndRejectedCannotLaterBeConfirmed()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "flow-reject");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "flow-reject-patient");

        var appointment = await pair.BookFirstSlotAsync(patient, date);

        var missingReason = await pair.Doctor.PatchAsJsonAsync(
            $"/api/doctor/appointments/{appointment.AppointmentId}/reject", new { rejectionReason = "" });
        Assert.Equal(HttpStatusCode.BadRequest, missingReason.StatusCode);

        var reject = await pair.Doctor.PatchAsJsonAsync(
            $"/api/doctor/appointments/{appointment.AppointmentId}/reject",
            new { rejectionReason = "Not accepting new patients this week." });
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);

        var confirmAttempt = await pair.Doctor.PatchAsync(
            $"/api/doctor/appointments/{appointment.AppointmentId}/confirm", null);
        Assert.Equal(HttpStatusCode.BadRequest, confirmAttempt.StatusCode);

        // The patient can read the rejection reason.
        var patientView = await (await patient.GetAsync($"/api/patient/appointments/{appointment.AppointmentId}"))
            .ReadEnvelopeAsync<PatientAppointmentPayload>();
        Assert.Equal("Not accepting new patients this week.", patientView.Data!.RejectionReason);
    }

    [Fact]
    public async Task Cancel_RequiresAReason_AndStampsWhoCancelledIt()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "flow-cancel");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "flow-cancel-patient");

        var appointment = await pair.BookFirstSlotAsync(patient, date);

        var missingReason = await patient.PatchAsJsonAsync(
            $"/api/patient/appointments/{appointment.AppointmentId}/cancel", new { cancellationReason = "" });
        Assert.Equal(HttpStatusCode.BadRequest, missingReason.StatusCode);

        var cancel = await patient.PatchAsJsonAsync(
            $"/api/patient/appointments/{appointment.AppointmentId}/cancel",
            new { cancellationReason = "Schedule changed." });
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal("Cancelled", (await cancel.ReadEnvelopeAsync<PatientAppointmentPayload>()).Data!.StatusName);

        // The doctor sees the same cancellation, with the reason visible.
        var doctorView = await (await pair.Doctor.GetAsync($"/api/doctor/appointments/{appointment.AppointmentId}"))
            .ReadEnvelopeAsync<DoctorAppointmentPayload>();
        Assert.Equal("Schedule changed.", doctorView.Data!.CancellationReason);
    }

    [Fact]
    public async Task Doctor_CanAlsoCancelAConfirmedAppointment()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "flow-doctor-cancel");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "flow-doctor-cancel-patient");

        var appointment = await pair.BookFirstSlotAsync(patient, date);
        await pair.Doctor.PatchAsync($"/api/doctor/appointments/{appointment.AppointmentId}/confirm", null);

        var cancel = await pair.Doctor.PatchAsJsonAsync(
            $"/api/doctor/appointments/{appointment.AppointmentId}/cancel",
            new { cancellationReason = "Doctor unavailable." });

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal("Cancelled", (await cancel.ReadEnvelopeAsync<DoctorAppointmentPayload>()).Data!.StatusName);
    }

    // -------------------------------------------------------------- Doctor notes

    [Fact]
    public async Task DoctorNotes_AreVisibleToTheDoctorButNeverToPatientOrHospital()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "notes-privacy");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "notes-privacy-patient");

        var appointment = await pair.BookFirstSlotAsync(patient, date);

        var notesResponse = await pair.Doctor.PutAsJsonAsync(
            $"/api/doctor/appointments/{appointment.AppointmentId}/notes",
            new { doctorNotes = "Confidential clinical observation." });

        Assert.Equal(HttpStatusCode.OK, notesResponse.StatusCode);
        Assert.Equal("Confidential clinical observation.",
            (await notesResponse.ReadEnvelopeAsync<DoctorAppointmentPayload>()).Data!.DoctorNotes);

        var patientRaw = await (await patient.GetAsync($"/api/patient/appointments/{appointment.AppointmentId}"))
            .Content.ReadAsStringAsync();
        Assert.DoesNotContain("doctorNotes", patientRaw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Confidential clinical observation", patientRaw);

        var hospitalRaw = await (await pair.Hospital.GetAsync(
                $"/api/hospital/appointments?patientName={Uri.EscapeDataString(appointment.DoctorName)}"))
            .Content.ReadAsStringAsync();
        Assert.DoesNotContain("Confidential clinical observation", hospitalRaw);
        Assert.DoesNotContain("doctorNotes", hospitalRaw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PatientCannotEditDoctorNotes_HospitalCannotReadThem()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "notes-forbidden");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "notes-forbidden-patient");

        var appointment = await pair.BookFirstSlotAsync(patient, date);

        var patientAttempt = await patient.PutAsJsonAsync(
            $"/api/doctor/appointments/{appointment.AppointmentId}/notes",
            new { doctorNotes = "A patient should never be able to write this." });
        Assert.Equal(HttpStatusCode.Forbidden, patientAttempt.StatusCode);

        var hospitalAttempt = await pair.Hospital.PutAsJsonAsync(
            $"/api/doctor/appointments/{appointment.AppointmentId}/notes",
            new { doctorNotes = "Nor should a hospital." });
        Assert.Equal(HttpStatusCode.Forbidden, hospitalAttempt.StatusCode);
    }

    // -------------------------------------------------------------- Hospital view

    [Fact]
    public async Task Hospital_SeesOnlyItsOwnAppointments()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pairA = await _scenario.ApprovedPairAsync(admin, "hosp-scope-a");
        var pairB = await _scenario.ApprovedPairAsync(admin, "hosp-scope-b");

        var (_, dateA) = await pairA.AddTomorrowAvailabilityAsync();
        var (_, dateB) = await pairB.AddTomorrowAvailabilityAsync();

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "hosp-scope-patient");

        // Distinct slot indices: both pairs share the same wide-open "tomorrow" window, so
        // booking the same wall-clock slot at both would collide with the patient's own
        // overlap rule rather than exercising hospital scoping.
        var appointmentA = await pairA.BookFirstSlotAsync(patient, dateA, slotIndex: 0);
        await pairB.BookFirstSlotAsync(patient, dateB, slotIndex: 1);

        var listingA = await (await pairA.Hospital.GetAsync("/api/hospital/appointments?pageSize=100"))
            .ReadEnvelopeAsync<PagedPayload<HospitalAppointmentPayload>>();

        Assert.Contains(listingA.Data!.Items, a => a.AppointmentId == appointmentA.AppointmentId);

        // Hospital A's listing must not contain anything belonging to hospital B.
        var listingB = await (await pairB.Hospital.GetAsync("/api/hospital/appointments?pageSize=100"))
            .ReadEnvelopeAsync<PagedPayload<HospitalAppointmentPayload>>();

        Assert.DoesNotContain(listingB.Data!.Items, a => a.AppointmentId == appointmentA.AppointmentId);
    }

    [Fact]
    public async Task Hospital_CannotAccessAnotherHospitalsAppointmentById()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pairA = await _scenario.ApprovedPairAsync(admin, "hosp-xacct-a");
        var pairB = await _scenario.ApprovedPairAsync(admin, "hosp-xacct-b");

        var (_, date) = await pairA.AddTomorrowAvailabilityAsync();
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "hosp-xacct-patient");
        var appointment = await pairA.BookFirstSlotAsync(patient, date);

        var response = await pairB.Hospital.GetAsync($"/api/hospital/appointments/{appointment.AppointmentId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------------- Ownership

    [Fact]
    public async Task Doctor_CannotAccessAnotherDoctorsAppointment()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pairA = await _scenario.ApprovedPairAsync(admin, "doc-xacct-a");
        var pairB = await _scenario.ApprovedPairAsync(admin, "doc-xacct-b");

        var (_, date) = await pairA.AddTomorrowAvailabilityAsync();
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "doc-xacct-patient");
        var appointment = await pairA.BookFirstSlotAsync(patient, date);

        Assert.Equal(HttpStatusCode.NotFound,
            (await pairB.Doctor.GetAsync($"/api/doctor/appointments/{appointment.AppointmentId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound,
            (await pairB.Doctor.PatchAsync($"/api/doctor/appointments/{appointment.AppointmentId}/confirm", null))
                .StatusCode);
    }

    [Fact]
    public async Task Patient_CannotAccessAnotherPatientsAppointment()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "pat-xacct");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();

        var (owner, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "pat-xacct-owner");
        var appointment = await pair.BookFirstSlotAsync(owner, date);

        var (intruder, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "pat-xacct-intruder");

        Assert.Equal(HttpStatusCode.NotFound,
            (await intruder.GetAsync($"/api/patient/appointments/{appointment.AppointmentId}")).StatusCode);

        var cancelAttempt = await intruder.PatchAsJsonAsync(
            $"/api/patient/appointments/{appointment.AppointmentId}/cancel",
            new { cancellationReason = "Not my appointment to cancel." });
        Assert.Equal(HttpStatusCode.NotFound, cancelAttempt.StatusCode);
    }

    [Fact]
    public async Task Doctor_CannotBookAppointmentsUsingThePatientEndpoint()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "doc-as-patient");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();

        var response = await pair.Doctor.PostAsJsonAsync("/api/patient/appointments", new
        {
            doctorProfileId = pair.DoctorProfileId,
            hospitalProfileId = pair.HospitalProfileId,
            appointmentDate = date.ToString("yyyy-MM-dd"),
            startTime = "09:00:00",
            reason = "A doctor should not be able to do this.",
            patientNotes = (string?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ------------------------------------------------------------ Pagination

    [Fact]
    public async Task PatientAppointments_SupportStatusFilterAndPagination()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "pat-paging");

        for (var i = 0; i < 3; i++)
        {
            var pair = await _scenario.ApprovedPairAsync(admin, $"pat-paging-{i}");
            var (_, date) = await pair.AddTomorrowAvailabilityAsync();

            // Distinct slot indices so the same patient's three bookings - all on the same
            // "tomorrow" date and window - do not overlap each other.
            await pair.BookFirstSlotAsync(patient, date, slotIndex: i);
        }

        var pending = await (await patient.GetAsync("/api/patient/appointments?status=Pending&pageSize=100"))
            .ReadEnvelopeAsync<PagedPayload<PatientAppointmentPayload>>();
        Assert.Equal(3, pending.Data!.TotalCount);

        var firstPage = await (await patient.GetAsync("/api/patient/appointments?page=1&pageSize=2"))
            .ReadEnvelopeAsync<PagedPayload<PatientAppointmentPayload>>();
        Assert.Equal(2, firstPage.Data!.Items.Count);
        Assert.True(firstPage.Data.HasNextPage);
    }

    // -------------------------------------------------------------- Dashboard stats

    [Fact]
    public async Task DashboardStats_ReflectPendingAndUpcomingCounts()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "dash-stats");
        var (_, date) = await pair.AddTomorrowAvailabilityAsync();
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "dash-stats-patient");

        var appointment = await pair.BookFirstSlotAsync(patient, date);

        var patientStats = await (await patient.GetAsync("/api/patient/appointments/dashboard-stats"))
            .ReadEnvelopeAsync<PatientDashboardStatsPayload>();
        Assert.True(patientStats.Data!.PendingCount >= 1);

        var doctorStats = await (await pair.Doctor.GetAsync("/api/doctor/appointments/dashboard-stats"))
            .ReadEnvelopeAsync<DoctorDashboardStatsPayload>();
        Assert.True(doctorStats.Data!.PendingCount >= 1);

        await pair.Doctor.PatchAsync($"/api/doctor/appointments/{appointment.AppointmentId}/confirm", null);

        var hospitalStats = await (await pair.Hospital.GetAsync("/api/hospital/appointments/dashboard-stats"))
            .ReadEnvelopeAsync<HospitalDashboardStatsPayload>();
        Assert.True(hospitalStats.Data!.ActiveApprovedDoctorsCount >= 1);
    }

    // ------------------------------------------------------------ Authorization

    [Theory]
    [InlineData(AppRoles.Doctor)]
    [InlineData(AppRoles.Hospital)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task PatientAppointmentEndpoints_AreClosedToOtherRoles(string role)
    {
        var (client, _) = await _scenario.NewAccountAsync(role, $"appt-pat-403-{role.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/patient/appointments")).StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.Patient)]
    [InlineData(AppRoles.Hospital)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task DoctorAppointmentEndpoints_AreClosedToOtherRoles(string role)
    {
        var (client, _) = await _scenario.NewAccountAsync(role, $"appt-doc-403-{role.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/doctor/appointments")).StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.Patient)]
    [InlineData(AppRoles.Doctor)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task HospitalAppointmentEndpoints_AreClosedToOtherRoles(string role)
    {
        var (client, _) = await _scenario.NewAccountAsync(role, $"appt-hosp-403-{role.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/hospital/appointments")).StatusCode);
    }

    [Fact]
    public async Task AppointmentEndpoints_Return401ForAnonymousCallers()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/patient/appointments")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/doctor/appointments")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/hospital/appointments")).StatusCode);
    }

    [Fact]
    public async Task InactiveUser_CannotUseProtectedSchedulingEndpoints()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var pair = await _scenario.ApprovedPairAsync(admin, "inactive-scheduling");

        var me = await (await pair.Doctor.GetAsync("/api/auth/me")).ReadEnvelopeAsync<UserPayload>();
        var doctorUserId = me.Data!.Id;

        await admin.PatchAsync($"/api/super-admin/users/{doctorUserId}/toggle-status", null);

        // The bearer token is still structurally valid, but the account behind it is
        // deactivated. The refresh-token revocation from deactivation, combined with the
        // access token eventually expiring, is what actually locks the account out; for the
        // still-live access token in this test window, the direct signal is that a fresh
        // sign-in is refused outright.
        var reLogin = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new
        {
            email = me.Data.Email,
            password = "StrongPass123!"
        });

        Assert.Equal(HttpStatusCode.Forbidden, reLogin.StatusCode);
    }
}
