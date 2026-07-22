using System.Net;
using System.Net.Http.Json;
using CareConnect.Domain.Constants;

namespace CareConnect.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class DirectoryEndpointsTests
{
    private readonly CareConnectApiFactory _factory;
    private readonly HealthcareScenario _scenario;

    public DirectoryEndpointsTests(CareConnectApiFactory factory)
    {
        _factory = factory;
        _scenario = new HealthcareScenario(factory);
    }

    // ---------------------------------------------------------- Visibility rules

    [Fact]
    public async Task Directory_ListsOnlyCompletedProfiles()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);

        var (_, _, completedHospital) =
            await _scenario.CompletedHospitalAsync([specialty.Id], "visible-hosp");

        // Registered but never completed, so it must stay out of the directory.
        var (incompleteClient, _) = await _scenario.NewAccountAsync(AppRoles.Hospital, "invisible-hosp");
        var incomplete = (await (await incompleteClient.GetAsync("/api/hospital/profile"))
            .ReadEnvelopeAsync<HospitalProfilePayload>()).Data!;

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "directory-visibility");

        var listing = await (await patient.GetAsync("/api/hospitals?pageSize=100"))
            .ReadEnvelopeAsync<PagedPayload<HospitalDirectoryPayload>>();

        Assert.Contains(listing.Data!.Items, h => h.Id == completedHospital.Id);
        Assert.DoesNotContain(listing.Data.Items, h => h.Id == incomplete.Id);

        // Fetching the incomplete one directly is a 404, not a partial record.
        Assert.Equal(HttpStatusCode.NotFound,
            (await patient.GetAsync($"/api/hospitals/{incomplete.Id}")).StatusCode);
    }

    [Fact]
    public async Task HospitalDetails_ShowsApprovedDoctorsAndHidesEveryOtherStatus()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);

        var (hospital, _, hospitalProfile) =
            await _scenario.CompletedHospitalAsync([specialty.Id], "details-hosp");

        var (approvedDoctor, _, approvedProfile) =
            await _scenario.CompletedDoctorAsync(specialty.Id, "details-approved");

        var (rejectedDoctor, _, rejectedProfile) =
            await _scenario.CompletedDoctorAsync(specialty.Id, "details-rejected");

        var (pendingDoctor, _, pendingProfile) =
            await _scenario.CompletedDoctorAsync(specialty.Id, "details-pending");

        var approvedRequest = await SendRequestAsync(approvedDoctor, hospitalProfile.Id);
        var rejectedRequest = await SendRequestAsync(rejectedDoctor, hospitalProfile.Id);
        await SendRequestAsync(pendingDoctor, hospitalProfile.Id);

        await hospital.PatchAsync($"/api/hospital/doctor-requests/{approvedRequest.Id}/approve", null);
        await hospital.PatchAsJsonAsync(
            $"/api/hospital/doctor-requests/{rejectedRequest.Id}/reject",
            new { rejectionReason = "Not a fit for the current roster." });

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "details-viewer");

        var details = (await (await patient.GetAsync($"/api/hospitals/{hospitalProfile.Id}"))
            .ReadEnvelopeAsync<HospitalDirectoryPayload>()).Data!;

        Assert.Contains(details.Doctors, d => d.DoctorProfileId == approvedProfile.Id);
        Assert.DoesNotContain(details.Doctors, d => d.DoctorProfileId == rejectedProfile.Id);
        Assert.DoesNotContain(details.Doctors, d => d.DoctorProfileId == pendingProfile.Id);

        Assert.Equal(1, details.NumberOfApprovedDoctors);
        Assert.Contains(details.Specialties, s => s.Id == specialty.Id);
    }

    [Fact]
    public async Task HospitalDetails_DropsADoctorOnceRemoved()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);

        var (hospital, _, hospitalProfile) =
            await _scenario.CompletedHospitalAsync([specialty.Id], "removed-hosp");

        var (doctor, _, doctorProfile) =
            await _scenario.CompletedDoctorAsync(specialty.Id, "removed-doc");

        var request = await SendRequestAsync(doctor, hospitalProfile.Id);
        await hospital.PatchAsync($"/api/hospital/doctor-requests/{request.Id}/approve", null);
        await hospital.PatchAsync($"/api/hospital/doctors/{doctorProfile.Id}/remove", null);

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "removed-viewer");

        var details = (await (await patient.GetAsync($"/api/hospitals/{hospitalProfile.Id}"))
            .ReadEnvelopeAsync<HospitalDirectoryPayload>()).Data!;

        Assert.DoesNotContain(details.Doctors, d => d.DoctorProfileId == doctorProfile.Id);
        Assert.Equal(0, details.NumberOfApprovedDoctors);
    }

    // ----------------------------------------------------------------- Filters

    [Fact]
    public async Task HospitalDirectory_FiltersBySearchLocationAndSpecialtyWithPaging()
    {
        var admin = await _scenario.SuperAdminClientAsync();

        var all = (await (await admin.GetAsync("/api/specialties"))
            .ReadEnvelopeAsync<List<SpecialtyOptionPayload>>()).Data!;

        var targetSpecialty = all[0];
        var otherSpecialty = all[1];
        var city = $"TestCity{Guid.NewGuid():N}"[..16];

        var (_, _, matching) = await _scenario.CompletedHospitalAsync(
            [targetSpecialty.Id], "filter-match", city);

        var (_, _, differentSpecialty) = await _scenario.CompletedHospitalAsync(
            [otherSpecialty.Id], "filter-other-spec", city);

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "hospital-filters");

        var byCity = await (await patient.GetAsync($"/api/hospitals?city={city}&pageSize=100"))
            .ReadEnvelopeAsync<PagedPayload<HospitalDirectoryPayload>>();
        Assert.Equal(2, byCity.Data!.TotalCount);

        var bySpecialty = await (await patient.GetAsync(
                $"/api/hospitals?city={city}&specialtyId={targetSpecialty.Id}"))
            .ReadEnvelopeAsync<PagedPayload<HospitalDirectoryPayload>>();

        Assert.Equal(1, bySpecialty.Data!.TotalCount);
        Assert.Equal(matching.Id, bySpecialty.Data.Items[0].Id);

        var byGovernorate = await (await patient.GetAsync(
                $"/api/hospitals?city={city}&governorate=Cairo"))
            .ReadEnvelopeAsync<PagedPayload<HospitalDirectoryPayload>>();
        Assert.Equal(2, byGovernorate.Data!.TotalCount);

        var noMatch = await (await patient.GetAsync($"/api/hospitals?city={city}&governorate=Aswan"))
            .ReadEnvelopeAsync<PagedPayload<HospitalDirectoryPayload>>();
        Assert.Equal(0, noMatch.Data!.TotalCount);

        var firstPage = await (await patient.GetAsync($"/api/hospitals?city={city}&page=1&pageSize=1"))
            .ReadEnvelopeAsync<PagedPayload<HospitalDirectoryPayload>>();

        Assert.Single(firstPage.Data!.Items);
        Assert.Equal(2, firstPage.Data.TotalPages);
        Assert.True(firstPage.Data.HasNextPage);
        Assert.False(firstPage.Data.HasPreviousPage);

        var secondPage = await (await patient.GetAsync($"/api/hospitals?city={city}&page=2&pageSize=1"))
            .ReadEnvelopeAsync<PagedPayload<HospitalDirectoryPayload>>();
        Assert.True(secondPage.Data!.HasPreviousPage);

        Assert.NotEqual(matching.Id, differentSpecialty.Id);
    }

    [Fact]
    public async Task DoctorDirectory_FiltersBySpecialtyHospitalAndLocation()
    {
        var admin = await _scenario.SuperAdminClientAsync();

        var all = (await (await admin.GetAsync("/api/specialties"))
            .ReadEnvelopeAsync<List<SpecialtyOptionPayload>>()).Data!;

        var specialtyA = all[0];
        var specialtyB = all[1];
        var city = $"DocCity{Guid.NewGuid():N}"[..15];

        var (affiliatedDoctor, _, affiliatedProfile) =
            await _scenario.CompletedDoctorAsync(specialtyA.Id, "dir-doc-a", city);

        var (_, _, unaffiliatedProfile) =
            await _scenario.CompletedDoctorAsync(specialtyB.Id, "dir-doc-b", city);

        var (hospital, _, hospitalProfile) =
            await _scenario.CompletedHospitalAsync([specialtyA.Id], "dir-hosp");

        var request = await SendRequestAsync(affiliatedDoctor, hospitalProfile.Id);
        await hospital.PatchAsync($"/api/hospital/doctor-requests/{request.Id}/approve", null);

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "doctor-filters");

        var byCity = await (await patient.GetAsync($"/api/doctors?city={city}&pageSize=100"))
            .ReadEnvelopeAsync<PagedPayload<DoctorDirectoryPayload>>();
        Assert.Equal(2, byCity.Data!.TotalCount);

        var bySpecialty = await (await patient.GetAsync(
                $"/api/doctors?city={city}&specialtyId={specialtyB.Id}"))
            .ReadEnvelopeAsync<PagedPayload<DoctorDirectoryPayload>>();

        Assert.Equal(1, bySpecialty.Data!.TotalCount);
        Assert.Equal(unaffiliatedProfile.Id, bySpecialty.Data.Items[0].DoctorProfileId);

        // hospitalId narrows to doctors approved at that hospital.
        var byHospital = await (await patient.GetAsync(
                $"/api/doctors?city={city}&hospitalId={hospitalProfile.Id}"))
            .ReadEnvelopeAsync<PagedPayload<DoctorDirectoryPayload>>();

        Assert.Equal(1, byHospital.Data!.TotalCount);
        Assert.Equal(affiliatedProfile.Id, byHospital.Data.Items[0].DoctorProfileId);
        Assert.Contains(byHospital.Data.Items[0].Hospitals, h => h.Id == hospitalProfile.Id);

        var paged = await (await patient.GetAsync($"/api/doctors?city={city}&page=1&pageSize=1"))
            .ReadEnvelopeAsync<PagedPayload<DoctorDirectoryPayload>>();

        Assert.Single(paged.Data!.Items);
        Assert.Equal(2, paged.Data.TotalPages);
    }

    [Fact]
    public async Task DoctorDirectory_ExcludesIncompleteAndDeactivatedDoctors()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);
        var city = $"HideCity{Guid.NewGuid():N}"[..16];

        var (_, deactivatedAuth, deactivatedProfile) =
            await _scenario.CompletedDoctorAsync(specialty.Id, "dir-deactivated", city);

        // Registered but never completed the profile.
        var (incompleteClient, _) = await _scenario.NewAccountAsync(AppRoles.Doctor, "dir-incomplete");
        var incompleteProfile = (await (await incompleteClient.GetAsync("/api/doctor/profile"))
            .ReadEnvelopeAsync<DoctorProfilePayload>()).Data!;

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "hidden-doctors");

        var beforeDeactivation = await (await patient.GetAsync($"/api/doctors?city={city}&pageSize=100"))
            .ReadEnvelopeAsync<PagedPayload<DoctorDirectoryPayload>>();

        Assert.Contains(beforeDeactivation.Data!.Items, d => d.DoctorProfileId == deactivatedProfile.Id);
        Assert.DoesNotContain(beforeDeactivation.Data.Items, d => d.DoctorProfileId == incompleteProfile.Id);

        // Deactivating the account hides the doctor from the directory.
        await admin.PatchAsync($"/api/super-admin/users/{deactivatedAuth.User.Id}/toggle-status", null);

        var afterDeactivation = await (await patient.GetAsync($"/api/doctors?city={city}&pageSize=100"))
            .ReadEnvelopeAsync<PagedPayload<DoctorDirectoryPayload>>();

        Assert.DoesNotContain(afterDeactivation.Data!.Items, d => d.DoctorProfileId == deactivatedProfile.Id);

        Assert.Equal(HttpStatusCode.NotFound,
            (await patient.GetAsync($"/api/doctors/{deactivatedProfile.Id}")).StatusCode);
    }

    // ------------------------------------------------------------ Details pages

    [Fact]
    public async Task DoctorDetails_ReturnsPublicFieldsAndApprovedHospitals()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var specialty = await _scenario.AnySpecialtyAsync(admin);

        var (doctor, _, doctorProfile) = await _scenario.CompletedDoctorAsync(specialty.Id, "doc-details");
        var (hospital, _, hospitalProfile) =
            await _scenario.CompletedHospitalAsync([specialty.Id], "doc-details-hosp");

        var request = await SendRequestAsync(doctor, hospitalProfile.Id);
        await hospital.PatchAsync($"/api/hospital/doctor-requests/{request.Id}/approve", null);
        await doctor.PatchAsync($"/api/doctor/hospitals/{hospitalProfile.Id}/set-primary", null);

        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "doc-details-viewer");

        var response = await patient.GetAsync($"/api/doctors/{doctorProfile.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = (await response.ReadEnvelopeAsync<DoctorDirectoryPayload>()).Data!;
        Assert.Equal(specialty.Id, details.Specialty!.Id);
        Assert.Equal(7, details.YearsOfExperience);
        Assert.Contains(details.Hospitals, h => h.Id == hospitalProfile.Id && h.IsPrimary);

        // Nothing private leaks out of the public endpoint.
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Directory_Returns404ForUnknownIds()
    {
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "directory-404");

        Assert.Equal(HttpStatusCode.NotFound,
            (await patient.GetAsync($"/api/hospitals/{Guid.NewGuid()}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound,
            (await patient.GetAsync($"/api/doctors/{Guid.NewGuid()}")).StatusCode);
    }

    // ---------------------------------------------------------- Access control

    [Theory]
    [InlineData(AppRoles.Patient)]
    [InlineData(AppRoles.Doctor)]
    [InlineData(AppRoles.Hospital)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task Directory_IsOpenToEveryAuthenticatedRole(string role)
    {
        var (client, _) = await _scenario.NewAccountAsync(role, $"dir-open-{role.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/hospitals")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/doctors")).StatusCode);
    }

    [Fact]
    public async Task Directory_Returns401ForAnonymousCallers()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/hospitals")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/doctors")).StatusCode);
    }

    [Fact]
    public async Task Directory_ClampsAnOversizedPageSize()
    {
        var (patient, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "page-clamp");

        var response = await (await patient.GetAsync("/api/hospitals?pageSize=5000"))
            .ReadEnvelopeAsync<PagedPayload<HospitalDirectoryPayload>>();

        Assert.Equal(100, response.Data!.PageSize);
    }

    // ------------------------------------------------------------------ Helper

    private static async Task<DoctorRequestPayload> SendRequestAsync(HttpClient doctor, Guid hospitalId)
    {
        var response = await doctor.PostAsJsonAsync(
            "/api/doctor/hospital-requests",
            new { hospitalProfileId = hospitalId });

        response.EnsureSuccessStatusCode();
        return (await response.ReadEnvelopeAsync<DoctorRequestPayload>()).Data!;
    }
}
