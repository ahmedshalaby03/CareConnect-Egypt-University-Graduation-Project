using System.Net;
using System.Net.Http.Json;
using CareConnect.Domain.Constants;

namespace CareConnect.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class AffiliationEndpointsTests
{
    private readonly CareConnectApiFactory _factory;
    private readonly HealthcareScenario _scenario;

    public AffiliationEndpointsTests(CareConnectApiFactory factory)
    {
        _factory = factory;
        _scenario = new HealthcareScenario(factory);
    }

    /// <summary>A completed doctor and a completed hospital that lists the doctor's specialty.</summary>
    private async Task<(HttpClient Doctor, HttpClient Hospital, Guid HospitalId, Guid DoctorProfileId, Guid SpecialtyId)>
        MatchedPairAsync(string prefix)
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);

        var (doctorClient, _, doctorProfile) =
            await _scenario.CompletedDoctorAsync(specialty.Id, $"{prefix}-doc");

        var (hospitalClient, _, hospitalProfile) =
            await _scenario.CompletedHospitalAsync([specialty.Id], $"{prefix}-hosp");

        return (doctorClient, hospitalClient, hospitalProfile.Id, doctorProfile.Id, specialty.Id);
    }

    private static Task<HttpResponseMessage> RequestAsync(HttpClient doctor, Guid hospitalId) =>
        doctor.PostAsJsonAsync("/api/doctor/hospital-requests", new { hospitalProfileId = hospitalId });

    // -------------------------------------------------------------- Requesting

    [Fact]
    public async Task Doctor_CanSendAnAffiliationRequest()
    {
        var (doctor, _, hospitalId, _, _) = await MatchedPairAsync("send");

        var response = await RequestAsync(doctor, hospitalId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = (await response.ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;
        Assert.Equal("Pending", created.StatusName);
        Assert.Equal(hospitalId, created.HospitalProfileId);
    }

    [Fact]
    public async Task Doctor_CannotRequestTwiceWhileOneIsPending()
    {
        var (doctor, _, hospitalId, _, _) = await MatchedPairAsync("duplicate");

        Assert.Equal(HttpStatusCode.Created, (await RequestAsync(doctor, hospitalId)).StatusCode);

        var second = await RequestAsync(doctor, hospitalId);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var envelope = await second.ReadEnvelopeAsync<object>();
        Assert.Contains("pending", envelope.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Doctor_CannotRequestAgainWhileAlreadyApproved()
    {
        var (doctor, hospital, hospitalId, _, _) = await MatchedPairAsync("approved-dup");

        var request = (await (await RequestAsync(doctor, hospitalId))
            .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        await hospital.PatchAsync($"/api/hospital/doctor-requests/{request.Id}/approve", null);

        var second = await RequestAsync(doctor, hospitalId);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("already affiliated",
            (await second.ReadEnvelopeAsync<object>()).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Doctor_MayReapplyAfterARejectionOrCancellation()
    {
        var (doctor, hospital, hospitalId, _, _) = await MatchedPairAsync("reapply");

        // Rejected, then allowed to try again.
        var first = (await (await RequestAsync(doctor, hospitalId))
            .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        await hospital.PatchAsJsonAsync(
            $"/api/hospital/doctor-requests/{first.Id}/reject",
            new { rejectionReason = "Not recruiting in this specialty right now." });

        var second = await RequestAsync(doctor, hospitalId);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        // Cancelled, then allowed again.
        var secondRequest = (await second.ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;
        await doctor.PatchAsync($"/api/doctor/hospital-requests/{secondRequest.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.Created, (await RequestAsync(doctor, hospitalId)).StatusCode);
    }

    [Fact]
    public async Task Doctor_MustCompleteTheirProfileBeforeRequesting()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);

        var (_, _, hospitalProfile) = await _scenario.CompletedHospitalAsync([specialty.Id], "incomplete-doc-hosp");

        // Registered but never filled the profile in.
        var (doctor, _) = await _scenario.NewAccountAsync(AppRoles.Doctor, "incomplete-doc");

        var response = await RequestAsync(doctor, hospitalProfile.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Complete your doctor profile",
            (await response.ReadEnvelopeAsync<object>()).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Doctor_CannotRequestAtAHospitalWithAnIncompleteProfile()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);

        var (doctor, _, _) = await _scenario.CompletedDoctorAsync(specialty.Id, "hosp-incomplete-doc");

        // A hospital that registered but never completed its profile.
        var (hospitalClient, _) = await _scenario.NewAccountAsync(AppRoles.Hospital, "incomplete-hosp");
        var hospitalProfile = (await (await hospitalClient.GetAsync("/api/hospital/profile"))
            .ReadEnvelopeAsync<HospitalProfilePayload>()).Data!;

        var response = await RequestAsync(doctor, hospitalProfile.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("has not finished setting up",
            (await response.ReadEnvelopeAsync<object>()).Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Doctor_IsRejectedWhenTheHospitalDoesNotOfferTheirSpecialty()
    {
        var admin = await _scenario.SuperAdminClientAsync();

        var all = (await (await admin.GetAsync("/api/specialties"))
            .ReadEnvelopeAsync<List<SpecialtyOptionPayload>>()).Data!;

        var doctorSpecialty = all[0];
        var hospitalSpecialty = all[1];

        var (doctor, _, _) = await _scenario.CompletedDoctorAsync(doctorSpecialty.Id, "mismatch-doc");
        var (_, _, hospital) = await _scenario.CompletedHospitalAsync([hospitalSpecialty.Id], "mismatch-hosp");

        var response = await RequestAsync(doctor, hospital.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var message = (await response.ReadEnvelopeAsync<object>()).Message;
        Assert.Contains("does not currently list", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(doctorSpecialty.Name, message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Doctor_Returns404ForAnUnknownHospital()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);
        var (doctor, _, _) = await _scenario.CompletedDoctorAsync(specialty.Id, "unknown-hosp");

        var response = await RequestAsync(doctor, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------------- Cancelling

    [Fact]
    public async Task Doctor_CanCancelAPendingRequestButNotAReviewedOne()
    {
        var (doctor, hospital, hospitalId, _, _) = await MatchedPairAsync("cancel");

        var request = (await (await RequestAsync(doctor, hospitalId))
            .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        var cancel = await doctor.PatchAsync($"/api/doctor/hospital-requests/{request.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal("Cancelled", (await cancel.ReadEnvelopeAsync<DoctorRequestPayload>()).Data!.StatusName);

        // Cancelling again is refused because the request is no longer pending.
        var again = await doctor.PatchAsync($"/api/doctor/hospital-requests/{request.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);

        // And an approved request cannot be cancelled by the doctor either.
        var second = (await (await RequestAsync(doctor, hospitalId))
            .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        await hospital.PatchAsync($"/api/hospital/doctor-requests/{second.Id}/approve", null);

        var cancelApproved = await doctor.PatchAsync($"/api/doctor/hospital-requests/{second.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.BadRequest, cancelApproved.StatusCode);
    }

    [Fact]
    public async Task Doctor_CannotCancelSomebodyElsesRequest()
    {
        var (doctor, _, hospitalId, _, specialtyId) = await MatchedPairAsync("cancel-other");

        var request = (await (await RequestAsync(doctor, hospitalId))
            .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        var (attacker, _, _) = await _scenario.CompletedDoctorAsync(specialtyId, "cancel-attacker");

        // Guessing the id must give the same answer as a genuinely missing row.
        var response = await attacker.PatchAsync($"/api/doctor/hospital-requests/{request.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // The victim's request is untouched.
        var listing = await (await doctor.GetAsync("/api/doctor/hospital-requests"))
            .ReadEnvelopeAsync<PagedPayload<DoctorRequestPayload>>();

        Assert.Equal("Pending", listing.Data!.Items.Single(r => r.Id == request.Id).StatusName);
    }

    // ------------------------------------------------------ Approve and reject

    [Fact]
    public async Task Hospital_CanApproveARequest()
    {
        var (doctor, hospital, hospitalId, doctorProfileId, _) = await MatchedPairAsync("approve");

        var request = (await (await RequestAsync(doctor, hospitalId))
            .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        var approve = await hospital.PatchAsync($"/api/hospital/doctor-requests/{request.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        var approved = (await approve.ReadEnvelopeAsync<HospitalRequestPayload>()).Data!;
        Assert.Equal("Approved", approved.StatusName);
        Assert.Equal(doctorProfileId, approved.DoctorProfileId);

        // The doctor now sees the hospital in their approved list.
        var hospitals = await (await doctor.GetAsync("/api/doctor/hospitals"))
            .ReadEnvelopeAsync<List<AffiliatedHospitalPayload>>();

        Assert.Contains(hospitals.Data!, h => h.Id == hospitalId && h.StatusName == "Approved");

        // And the hospital sees the doctor in its roster.
        var doctors = await (await hospital.GetAsync("/api/hospital/doctors"))
            .ReadEnvelopeAsync<PagedPayload<HospitalDoctorPayload>>();

        Assert.Contains(doctors.Data!.Items, d => d.DoctorProfileId == doctorProfileId);
    }

    [Fact]
    public async Task Hospital_CanRejectARequestButOnlyWithAReason()
    {
        var (doctor, hospital, hospitalId, _, _) = await MatchedPairAsync("reject");

        var request = (await (await RequestAsync(doctor, hospitalId))
            .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        var missingReason = await hospital.PatchAsJsonAsync(
            $"/api/hospital/doctor-requests/{request.Id}/reject",
            new { rejectionReason = "" });

        Assert.Equal(HttpStatusCode.BadRequest, missingReason.StatusCode);
        Assert.Contains((await missingReason.ReadEnvelopeAsync<object>()).Errors!,
            e => e.Contains("rejection reason", StringComparison.OrdinalIgnoreCase));

        var reject = await hospital.PatchAsJsonAsync(
            $"/api/hospital/doctor-requests/{request.Id}/reject",
            new { rejectionReason = "License verification is still outstanding." });

        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);

        var rejected = (await reject.ReadEnvelopeAsync<HospitalRequestPayload>()).Data!;
        Assert.Equal("Rejected", rejected.StatusName);

        // The doctor can read the reason.
        var listing = await (await doctor.GetAsync("/api/doctor/hospital-requests?status=Rejected"))
            .ReadEnvelopeAsync<PagedPayload<DoctorRequestPayload>>();

        Assert.Equal("License verification is still outstanding.",
            listing.Data!.Items.Single(r => r.Id == request.Id).RejectionReason);
    }

    [Fact]
    public async Task Hospital_CannotApproveARequestSentToAnotherHospital()
    {
        var (doctor, _, hospitalId, _, specialtyId) = await MatchedPairAsync("wrong-hospital");

        var request = (await (await RequestAsync(doctor, hospitalId))
            .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        var (bystander, _, _) = await _scenario.CompletedHospitalAsync([specialtyId], "bystander-hosp");

        Assert.Equal(HttpStatusCode.NotFound,
            (await bystander.PatchAsync($"/api/hospital/doctor-requests/{request.Id}/approve", null)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound,
            (await bystander.PatchAsJsonAsync($"/api/hospital/doctor-requests/{request.Id}/reject",
                new { rejectionReason = "Trying to reject a request that is not mine." })).StatusCode);
    }

    [Fact]
    public async Task Hospital_CannotApproveARequestTwice()
    {
        var (doctor, hospital, hospitalId, _, _) = await MatchedPairAsync("double-approve");

        var request = (await (await RequestAsync(doctor, hospitalId))
            .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        await hospital.PatchAsync($"/api/hospital/doctor-requests/{request.Id}/approve", null);

        var again = await hospital.PatchAsync($"/api/hospital/doctor-requests/{request.Id}/approve", null);
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
    }

    // ----------------------------------------------------------------- Removal

    [Fact]
    public async Task Hospital_CanRemoveAnApprovedDoctorWithoutLosingTheHistory()
    {
        var (doctor, hospital, hospitalId, doctorProfileId, _) = await MatchedPairAsync("remove");

        var request = (await (await RequestAsync(doctor, hospitalId))
            .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        await hospital.PatchAsync($"/api/hospital/doctor-requests/{request.Id}/approve", null);

        var remove = await hospital.PatchAsync($"/api/hospital/doctors/{doctorProfileId}/remove", null);
        Assert.Equal(HttpStatusCode.OK, remove.StatusCode);
        Assert.Equal("Removed", (await remove.ReadEnvelopeAsync<HospitalRequestPayload>()).Data!.StatusName);

        // Gone from the active roster...
        var roster = await (await hospital.GetAsync("/api/hospital/doctors"))
            .ReadEnvelopeAsync<PagedPayload<HospitalDoctorPayload>>();
        Assert.DoesNotContain(roster.Data!.Items, d => d.DoctorProfileId == doctorProfileId);

        // ...but the record survives as history.
        var history = await (await hospital.GetAsync("/api/hospital/doctor-requests?status=Removed"))
            .ReadEnvelopeAsync<PagedPayload<HospitalRequestPayload>>();
        Assert.Contains(history.Data!.Items, r => r.Id == request.Id);
    }

    [Fact]
    public async Task Hospital_CannotRemoveADoctorItNeverApproved()
    {
        var (_, hospital, _, doctorProfileId, _) = await MatchedPairAsync("remove-unrelated");

        var response = await hospital.PatchAsync($"/api/hospital/doctors/{doctorProfileId}/remove", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------------ Primary flag

    [Fact]
    public async Task Doctor_HasAtMostOnePrimaryHospital()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);

        var (doctor, _, _) = await _scenario.CompletedDoctorAsync(specialty.Id, "primary-doc");
        var (hospitalA, _, profileA) = await _scenario.CompletedHospitalAsync([specialty.Id], "primary-a");
        var (hospitalB, _, profileB) = await _scenario.CompletedHospitalAsync([specialty.Id], "primary-b");

        foreach (var (client, hospitalId) in new[] { (hospitalA, profileA.Id), (hospitalB, profileB.Id) })
        {
            var request = (await (await RequestAsync(doctor, hospitalId))
                .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

            await client.PatchAsync($"/api/hospital/doctor-requests/{request.Id}/approve", null);
        }

        await doctor.PatchAsync($"/api/doctor/hospitals/{profileA.Id}/set-primary", null);

        var afterFirst = (await (await doctor.GetAsync("/api/doctor/hospitals"))
            .ReadEnvelopeAsync<List<AffiliatedHospitalPayload>>()).Data!;

        Assert.True(afterFirst.Single(h => h.Id == profileA.Id).IsPrimary);
        Assert.False(afterFirst.Single(h => h.Id == profileB.Id).IsPrimary);

        // Switching primary must clear the previous one, never leave two.
        await doctor.PatchAsync($"/api/doctor/hospitals/{profileB.Id}/set-primary", null);

        var afterSwitch = (await (await doctor.GetAsync("/api/doctor/hospitals"))
            .ReadEnvelopeAsync<List<AffiliatedHospitalPayload>>()).Data!;

        Assert.Single(afterSwitch.Where(h => h.IsPrimary));
        Assert.True(afterSwitch.Single(h => h.Id == profileB.Id).IsPrimary);
    }

    [Fact]
    public async Task Doctor_CannotSetAHospitalPrimaryWithoutAnApprovedAffiliation()
    {
        var (doctor, _, hospitalId, _, _) = await MatchedPairAsync("primary-pending");

        // Pending only, never approved.
        await RequestAsync(doctor, hospitalId);

        var response = await doctor.PatchAsync($"/api/doctor/hospitals/{hospitalId}/set-primary", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // -------------------------------------------------------------- Filtering

    [Fact]
    public async Task HospitalRequests_CanBeFilteredByStatusNameAndSpecialty()
    {
        var admin = await _scenario.SuperAdminClientAsync();

        var all = (await (await admin.GetAsync("/api/specialties"))
            .ReadEnvelopeAsync<List<SpecialtyOptionPayload>>()).Data!;

        var specialtyA = all[0];
        var specialtyB = all[1];

        var (hospital, _, hospitalProfile) =
            await _scenario.CompletedHospitalAsync([specialtyA.Id, specialtyB.Id], "filter-hosp");

        var (doctorA, _, _) = await _scenario.CompletedDoctorAsync(specialtyA.Id, "filter-doc-a");
        var (doctorB, _, _) = await _scenario.CompletedDoctorAsync(specialtyB.Id, "filter-doc-b");

        await RequestAsync(doctorA, hospitalProfile.Id);
        var requestB = (await (await RequestAsync(doctorB, hospitalProfile.Id))
            .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        await hospital.PatchAsync($"/api/hospital/doctor-requests/{requestB.Id}/approve", null);

        var pending = await (await hospital.GetAsync("/api/hospital/doctor-requests?status=Pending"))
            .ReadEnvelopeAsync<PagedPayload<HospitalRequestPayload>>();
        Assert.Equal(1, pending.Data!.TotalCount);

        var approved = await (await hospital.GetAsync("/api/hospital/doctor-requests?status=Approved"))
            .ReadEnvelopeAsync<PagedPayload<HospitalRequestPayload>>();
        Assert.Equal(1, approved.Data!.TotalCount);

        var bySpecialty = await (await hospital.GetAsync(
                $"/api/hospital/doctor-requests?specialtyId={specialtyA.Id}"))
            .ReadEnvelopeAsync<PagedPayload<HospitalRequestPayload>>();

        Assert.Equal(1, bySpecialty.Data!.TotalCount);
        Assert.Equal(specialtyA.Id, bySpecialty.Data.Items[0].Specialty!.Id);

        // The request list carries the professional details a hospital needs to decide.
        var row = bySpecialty.Data.Items[0];
        Assert.NotNull(row.LicenseNumber);
        Assert.Equal(7, row.YearsOfExperience);
        Assert.NotEqual(default, row.RequestedAt);

        var byName = await (await hospital.GetAsync(
                $"/api/hospital/doctor-requests?search={row.DoctorName}"))
            .ReadEnvelopeAsync<PagedPayload<HospitalRequestPayload>>();
        Assert.True(byName.Data!.TotalCount >= 1);
    }

    [Fact]
    public async Task DoctorRequests_CanBeFilteredByStatusAndHospitalName()
    {
        var (doctor, hospital, hospitalId, _, _) = await MatchedPairAsync("doc-filter");

        var request = (await (await RequestAsync(doctor, hospitalId))
            .ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;

        var pending = await (await doctor.GetAsync("/api/doctor/hospital-requests?status=Pending"))
            .ReadEnvelopeAsync<PagedPayload<DoctorRequestPayload>>();
        Assert.Contains(pending.Data!.Items, r => r.Id == request.Id);

        var byName = await (await doctor.GetAsync(
                $"/api/doctor/hospital-requests?hospitalName={Uri.EscapeDataString(request.HospitalName)}"))
            .ReadEnvelopeAsync<PagedPayload<DoctorRequestPayload>>();
        Assert.Contains(byName.Data!.Items, r => r.Id == request.Id);

        await hospital.PatchAsync($"/api/hospital/doctor-requests/{request.Id}/approve", null);

        var stillPending = await (await doctor.GetAsync("/api/doctor/hospital-requests?status=Pending"))
            .ReadEnvelopeAsync<PagedPayload<DoctorRequestPayload>>();
        Assert.DoesNotContain(stillPending.Data!.Items, r => r.Id == request.Id);
    }

    // ---------------------------------------------------------- Authorization

    [Theory]
    [InlineData(AppRoles.Patient)]
    [InlineData(AppRoles.Hospital)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task DoctorAffiliationEndpoints_AreClosedToOtherRoles(string role)
    {
        var (client, _) = await _scenario.NewAccountAsync(role, $"aff-doc-403-{role.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("/api/doctor/hospital-requests")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("/api/doctor/hospital-requests",
                new { hospitalProfileId = Guid.NewGuid() })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/doctor/hospitals")).StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.Patient)]
    [InlineData(AppRoles.Doctor)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task HospitalAffiliationEndpoints_AreClosedToOtherRoles(string role)
    {
        var (client, _) = await _scenario.NewAccountAsync(role, $"aff-hosp-403-{role.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("/api/hospital/doctor-requests")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PatchAsync($"/api/hospital/doctor-requests/{Guid.NewGuid()}/approve", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/hospital/doctors")).StatusCode);
    }
}
