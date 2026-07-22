using System.Net;
using System.Net.Http.Json;
using CareConnect.Domain.Constants;

namespace CareConnect.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class ProfileEndpointsTests
{
    private readonly CareConnectApiFactory _factory;
    private readonly HealthcareScenario _scenario;

    public ProfileEndpointsTests(CareConnectApiFactory factory)
    {
        _factory = factory;
        _scenario = new HealthcareScenario(factory);
    }

    // ------------------------------------------------------------ Doctor profile

    [Fact]
    public async Task DoctorProfile_StartsEmptyAndIncompleteAfterRegistration()
    {
        var (client, _) = await _scenario.NewAccountAsync(AppRoles.Doctor, "fresh-doctor");

        var envelope = await (await client.GetAsync("/api/doctor/profile"))
            .ReadEnvelopeAsync<DoctorProfilePayload>();

        Assert.False(envelope.Data!.IsProfileCompleted);
        Assert.Null(envelope.Data.Specialty);

        // The UI uses this list to tell the doctor exactly what is still missing.
        Assert.Contains("Specialty", envelope.Data.MissingFields);
        Assert.Contains("License number", envelope.Data.MissingFields);
        Assert.Contains("Governorate", envelope.Data.MissingFields);
    }

    [Fact]
    public async Task DoctorProfile_BecomesCompletedOnlyWhenEveryRequiredFieldIsPresent()
    {
        var (client, auth) = await _scenario.NewAccountAsync(AppRoles.Doctor, "completion");
        var specialty = await _scenario.AnySpecialtyAsync(client);

        // Missing city, so it must stay incomplete.
        var partial = await client.PutAsJsonAsync("/api/doctor/profile", new
        {
            fullName = auth.User.FullName,
            specialtyId = specialty.Id,
            licenseNumber = "LIC-001",
            yearsOfExperience = 3,
            governorate = "Giza"
        });

        var partialProfile = (await partial.ReadEnvelopeAsync<DoctorProfilePayload>()).Data!;
        Assert.False(partialProfile.IsProfileCompleted);
        Assert.Contains("City", partialProfile.MissingFields);

        var complete = await client.PutAsJsonAsync("/api/doctor/profile", new
        {
            fullName = auth.User.FullName,
            specialtyId = specialty.Id,
            licenseNumber = "LIC-001",
            yearsOfExperience = 3,
            governorate = "Giza",
            city = "Dokki"
        });

        var completeProfile = (await complete.ReadEnvelopeAsync<DoctorProfilePayload>()).Data!;
        Assert.True(completeProfile.IsProfileCompleted);
        Assert.Empty(completeProfile.MissingFields);
        Assert.Equal(specialty.Id, completeProfile.Specialty!.Id);
    }

    [Fact]
    public async Task DoctorProfile_IgnoresAClientAttemptToClaimCompletion()
    {
        var (client, auth) = await _scenario.NewAccountAsync(AppRoles.Doctor, "forged-completion");

        // isProfileCompleted is not part of the request contract; sending it must change nothing.
        var response = await client.PutAsJsonAsync("/api/doctor/profile", new
        {
            fullName = auth.User.FullName,
            biography = "Trying to look complete.",
            isProfileCompleted = true
        });

        var profile = (await response.ReadEnvelopeAsync<DoctorProfilePayload>()).Data!;
        Assert.False(profile.IsProfileCompleted);
    }

    [Fact]
    public async Task DoctorProfile_UpdatesTheLinkedAccountNameAndPhone()
    {
        var (client, _) = await _scenario.NewAccountAsync(AppRoles.Doctor, "account-sync");
        var specialty = await _scenario.AnySpecialtyAsync(client);
        var phone = $"+2011{Random.Shared.Next(10_000_000, 99_999_999)}";

        await client.PutAsJsonAsync("/api/doctor/profile", new
        {
            fullName = "Dr Mona Kamal",
            phoneNumber = phone,
            specialtyId = specialty.Id,
            licenseNumber = "LIC-777",
            yearsOfExperience = 12,
            governorate = "Alexandria",
            city = "Smouha"
        });

        // The change is visible through the auth endpoint, proving ApplicationUser was updated.
        var me = await (await client.GetAsync("/api/auth/me")).ReadEnvelopeAsync<UserPayload>();
        Assert.Equal("Dr Mona Kamal", me.Data!.FullName);
        Assert.Equal(phone, me.Data.PhoneNumber);
    }

    [Fact]
    public async Task DoctorProfile_RejectsAPhoneNumberAnotherAccountAlreadyUses()
    {
        var phone = $"+2012{Random.Shared.Next(10_000_000, 99_999_999)}";

        var (first, _) = await _scenario.NewAccountAsync(AppRoles.Doctor, "phone-owner");
        await first.PutAsJsonAsync("/api/doctor/profile", new { phoneNumber = phone });

        var (second, _) = await _scenario.NewAccountAsync(AppRoles.Doctor, "phone-thief");
        var response = await second.PutAsJsonAsync("/api/doctor/profile", new { phoneNumber = phone });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DoctorProfile_RejectsNegativeExperienceAndPrice()
    {
        var (client, _) = await _scenario.NewAccountAsync(AppRoles.Doctor, "negative-values");

        var response = await client.PutAsJsonAsync("/api/doctor/profile", new
        {
            yearsOfExperience = -3,
            consultationPrice = -100
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var envelope = await response.ReadEnvelopeAsync<object>();
        Assert.Contains(envelope.Errors!, e => e.Contains("experience", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(envelope.Errors!, e => e.Contains("negative", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DoctorProfile_RejectsAnUnknownOrInactiveSpecialty()
    {
        var (client, _) = await _scenario.NewAccountAsync(AppRoles.Doctor, "bad-specialty");

        var unknown = await client.PutAsJsonAsync("/api/doctor/profile", new
        {
            specialtyId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

        // Now an existing but deactivated one.
        var admin = await _scenario.SuperAdminClientAsync();
        var created = (await (await admin.PostAsJsonAsync("/api/super-admin/specialties", new
        {
            name = $"Retired Specialty {Guid.NewGuid():N}"[..30]
        })).ReadEnvelopeAsync<SpecialtyPayload>()).Data!;

        await admin.PatchAsync($"/api/super-admin/specialties/{created.Id}/toggle-status", null);

        var inactive = await client.PutAsJsonAsync("/api/doctor/profile", new { specialtyId = created.Id });
        Assert.Equal(HttpStatusCode.BadRequest, inactive.StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.Patient)]
    [InlineData(AppRoles.Hospital)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task DoctorProfileEndpoints_AreClosedToOtherRoles(string role)
    {
        var (client, _) = await _scenario.NewAccountAsync(role, $"doctor-403-{role.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/doctor/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync("/api/doctor/profile", new { })).StatusCode);
    }

    [Fact]
    public async Task DoctorProfile_OneDoctorsUpdateNeverTouchesAnother()
    {
        var (clientA, authA) = await _scenario.NewAccountAsync(AppRoles.Doctor, "doctor-a");
        var (clientB, authB) = await _scenario.NewAccountAsync(AppRoles.Doctor, "doctor-b");

        await clientA.PutAsJsonAsync("/api/doctor/profile", new
        {
            fullName = "Doctor A Updated",
            biography = "Doctor A biography."
        });

        var profileB = await (await clientB.GetAsync("/api/doctor/profile"))
            .ReadEnvelopeAsync<DoctorProfilePayload>();

        // The route carries no id at all, so B is untouched by A's write.
        Assert.NotEqual("Doctor A Updated", profileB.Data!.FullName);
        Assert.Null(profileB.Data.Biography);
        Assert.Equal(authB.User.Email, profileB.Data.Email);
        Assert.NotEqual(authA.User.Id, authB.User.Id);
    }

    // ---------------------------------------------------------- Hospital profile

    [Fact]
    public async Task HospitalProfile_BecomesCompletedWhenTheRequiredFieldsArePresent()
    {
        var (client, _) = await _scenario.NewAccountAsync(AppRoles.Hospital, "hospital-completion");

        var initial = await (await client.GetAsync("/api/hospital/profile"))
            .ReadEnvelopeAsync<HospitalProfilePayload>();

        Assert.False(initial.Data!.IsProfileCompleted);
        Assert.Contains("Hospital name", initial.Data.MissingFields);

        var partial = await client.PutAsJsonAsync("/api/hospital/profile", new
        {
            hospitalName = "Al Salam Hospital",
            address = "1 Corniche Road",
            governorate = "Cairo"
        });

        var partialProfile = (await partial.ReadEnvelopeAsync<HospitalProfilePayload>()).Data!;
        Assert.False(partialProfile.IsProfileCompleted);
        Assert.Contains("City", partialProfile.MissingFields);
        Assert.Contains("Phone number", partialProfile.MissingFields);

        var complete = await client.PutAsJsonAsync("/api/hospital/profile", new
        {
            hospitalName = "Al Salam Hospital",
            address = "1 Corniche Road",
            governorate = "Cairo",
            city = "Maadi",
            phoneNumber = "+20227000000",
            openingTime = "09:00",
            closingTime = "21:30"
        });

        var completeProfile = (await complete.ReadEnvelopeAsync<HospitalProfilePayload>()).Data!;
        Assert.True(completeProfile.IsProfileCompleted);
        Assert.Empty(completeProfile.MissingFields);
        Assert.Equal("09:00", completeProfile.OpeningTime);
        Assert.Equal("21:30", completeProfile.ClosingTime);
    }

    [Fact]
    public async Task HospitalProfile_RejectsAMalformedTimeAndUrl()
    {
        var (client, _) = await _scenario.NewAccountAsync(AppRoles.Hospital, "hospital-bad-input");

        var response = await client.PutAsJsonAsync("/api/hospital/profile", new
        {
            hospitalName = "Bad Input Hospital",
            openingTime = "9am",
            websiteUrl = "not-a-url"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var envelope = await response.ReadEnvelopeAsync<object>();
        Assert.Contains(envelope.Errors!, e => e.Contains("HH:mm", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(envelope.Errors!, e => e.Contains("Website URL", StringComparison.OrdinalIgnoreCase));
    }

    // ------------------------------------------------------- Hospital specialties

    [Fact]
    public async Task HospitalSpecialties_CanSelectSeveralAndReplaceTheSetLater()
    {
        var (client, _) = await _scenario.NewAccountAsync(AppRoles.Hospital, "hospital-specialties");

        var all = (await (await client.GetAsync("/api/specialties"))
            .ReadEnvelopeAsync<List<SpecialtyOptionPayload>>()).Data!;

        var firstThree = all.Take(3).Select(s => s.Id).ToArray();

        var added = await client.PutAsJsonAsync(
            "/api/hospital/profile/specialties",
            new { specialtyIds = firstThree });

        Assert.Equal(HttpStatusCode.OK, added.StatusCode);

        var withThree = (await added.ReadEnvelopeAsync<HospitalProfilePayload>()).Data!;
        Assert.Equal(3, withThree.Specialties.Count);

        // Replacing with a smaller set removes the others without touching the Specialty rows.
        var replacement = all.Skip(1).Take(1).Select(s => s.Id).ToArray();

        var replaced = await client.PutAsJsonAsync(
            "/api/hospital/profile/specialties",
            new { specialtyIds = replacement });

        var withOne = (await replaced.ReadEnvelopeAsync<HospitalProfilePayload>()).Data!;
        Assert.Single(withOne.Specialties);
        Assert.Equal(replacement[0], withOne.Specialties[0].Id);

        // The global specialty list is intact.
        var stillThere = (await (await client.GetAsync("/api/specialties"))
            .ReadEnvelopeAsync<List<SpecialtyOptionPayload>>()).Data!;

        Assert.Equal(all.Count, stillThere.Count);
    }

    [Fact]
    public async Task HospitalSpecialties_RejectsDuplicatesUnknownIdsAndInactiveOnes()
    {
        var (client, _) = await _scenario.NewAccountAsync(AppRoles.Hospital, "hospital-bad-specialties");
        var specialty = await _scenario.AnySpecialtyAsync(client);

        var duplicated = await client.PutAsJsonAsync(
            "/api/hospital/profile/specialties",
            new { specialtyIds = new[] { specialty.Id, specialty.Id } });

        Assert.Equal(HttpStatusCode.BadRequest, duplicated.StatusCode);

        var unknown = await client.PutAsJsonAsync(
            "/api/hospital/profile/specialties",
            new { specialtyIds = new[] { Guid.NewGuid() } });

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

        var admin = await _scenario.SuperAdminClientAsync();
        var retired = (await (await admin.PostAsJsonAsync("/api/super-admin/specialties", new
        {
            name = $"Unavailable {Guid.NewGuid():N}"[..24]
        })).ReadEnvelopeAsync<SpecialtyPayload>()).Data!;

        await admin.PatchAsync($"/api/super-admin/specialties/{retired.Id}/toggle-status", null);

        var inactive = await client.PutAsJsonAsync(
            "/api/hospital/profile/specialties",
            new { specialtyIds = new[] { retired.Id } });

        Assert.Equal(HttpStatusCode.BadRequest, inactive.StatusCode);
    }

    [Fact]
    public async Task HospitalSpecialties_OneHospitalCannotAffectAnother()
    {
        var (clientA, _) = await _scenario.NewAccountAsync(AppRoles.Hospital, "hospital-x");
        var (clientB, _) = await _scenario.NewAccountAsync(AppRoles.Hospital, "hospital-y");

        var specialty = await _scenario.AnySpecialtyAsync(clientA);

        await clientA.PutAsJsonAsync(
            "/api/hospital/profile/specialties",
            new { specialtyIds = new[] { specialty.Id } });

        var profileB = await (await clientB.GetAsync("/api/hospital/profile"))
            .ReadEnvelopeAsync<HospitalProfilePayload>();

        Assert.Empty(profileB.Data!.Specialties);
    }

    [Theory]
    [InlineData(AppRoles.Patient)]
    [InlineData(AppRoles.Doctor)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task HospitalProfileEndpoints_AreClosedToOtherRoles(string role)
    {
        var (client, _) = await _scenario.NewAccountAsync(role, $"hospital-403-{role.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/hospital/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync("/api/hospital/profile", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync("/api/hospital/profile/specialties",
                new { specialtyIds = Array.Empty<Guid>() })).StatusCode);
    }

    [Fact]
    public async Task ProfileEndpoints_Return401WithoutAToken()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/doctor/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/hospital/profile")).StatusCode);
    }
}
