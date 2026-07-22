using System.Net;
using System.Net.Http.Json;
using CareConnect.Domain.Constants;

namespace CareConnect.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class SpecialtyEndpointsTests
{
    private readonly CareConnectApiFactory _factory;
    private readonly HealthcareScenario _scenario;

    public SpecialtyEndpointsTests(CareConnectApiFactory factory)
    {
        _factory = factory;
        _scenario = new HealthcareScenario(factory);
    }

    // ------------------------------------------------------------------ Seeding

    [Fact]
    public async Task Seeding_CreatesTheTwelveStartingSpecialtiesWithArabicNames()
    {
        var (client, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "seed-check");

        var envelope = await (await client.GetAsync("/api/specialties"))
            .ReadEnvelopeAsync<List<SpecialtyOptionPayload>>();

        var names = envelope.Data!.Select(s => s.Name).ToList();

        string[] expected =
        [
            "General Medicine", "Cardiology", "Dermatology", "Pediatrics", "Orthopedics",
            "Obstetrics and Gynecology", "Dentistry", "Neurology", "Ophthalmology", "ENT",
            "Psychiatry", "General Surgery"
        ];

        foreach (var specialty in expected)
        {
            Assert.Contains(specialty, names);
        }

        // Every seeded specialty carries an Arabic label.
        var cardiology = envelope.Data!.First(s => s.Name == "Cardiology");
        Assert.Equal("أمراض القلب", cardiology.ArabicName);
    }

    [Fact]
    public async Task GetSpecialties_IsSortedAlphabeticallyAndRequiresAuthentication()
    {
        var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/specialties")).StatusCode);

        // Any signed-in role may read the list, not just doctors.
        var (client, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "specialty-sort");

        var envelope = await (await client.GetAsync("/api/specialties"))
            .ReadEnvelopeAsync<List<SpecialtyOptionPayload>>();

        var names = envelope.Data!.Select(s => s.Name).ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal).ToList(), names);
    }

    [Fact]
    public async Task GetSpecialties_HidesInactiveOnesFromPublicForms()
    {
        var admin = await _scenario.SuperAdminClientAsync();

        var created = (await (await admin.PostAsJsonAsync("/api/super-admin/specialties", new
        {
            name = $"Hidden Specialty {Guid.NewGuid():N}"[..28],
            arabicName = $"تخصص {Guid.NewGuid():N}"[..14],
            description = "Deactivated straight away."
        })).ReadEnvelopeAsync<SpecialtyPayload>()).Data!;

        await admin.PatchAsync($"/api/super-admin/specialties/{created.Id}/toggle-status", null);

        var (client, _) = await _scenario.NewAccountAsync(AppRoles.Patient, "inactive-hidden");

        var envelope = await (await client.GetAsync("/api/specialties"))
            .ReadEnvelopeAsync<List<SpecialtyOptionPayload>>();

        Assert.DoesNotContain(envelope.Data!, s => s.Id == created.Id);
    }

    // -------------------------------------------------------- SuperAdmin CRUD

    [Fact]
    public async Task SuperAdmin_CanCreateAndEditASpecialty()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await admin.PostAsJsonAsync("/api/super-admin/specialties", new
        {
            name = $"Vascular Surgery {suffix}",
            arabicName = $"جراحة الأوعية {suffix}",
            description = "Blood vessel surgery."
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = (await createResponse.ReadEnvelopeAsync<SpecialtyPayload>()).Data!;
        Assert.True(created.IsActive);
        Assert.Equal($"Vascular Surgery {suffix}", created.Name);

        var updateResponse = await admin.PutAsJsonAsync(
            $"/api/super-admin/specialties/{created.Id}",
            new
            {
                name = $"Vascular and Endovascular Surgery {suffix}",
                arabicName = $"جراحة الأوعية الدموية {suffix}",
                description = "Updated description."
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = (await updateResponse.ReadEnvelopeAsync<SpecialtyPayload>()).Data!;
        Assert.Equal($"Vascular and Endovascular Surgery {suffix}", updated.Name);
        Assert.Equal("Updated description.", updated.Description);
    }

    [Fact]
    public async Task SuperAdmin_CannotCreateADuplicateEnglishOrArabicName()
    {
        var admin = await _scenario.SuperAdminClientAsync();

        var duplicateEnglish = await admin.PostAsJsonAsync("/api/super-admin/specialties", new
        {
            name = "Cardiology",
            arabicName = (string?)null
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateEnglish.StatusCode);

        var duplicateArabic = await admin.PostAsJsonAsync("/api/super-admin/specialties", new
        {
            name = $"Something New {Guid.NewGuid():N}"[..24],
            arabicName = "أمراض القلب"
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicateArabic.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_CanKeepTheSameNameWhenEditingOtherFields()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var name = $"Nuclear Medicine {Guid.NewGuid():N}"[..26];

        var created = (await (await admin.PostAsJsonAsync("/api/super-admin/specialties", new { name }))
            .ReadEnvelopeAsync<SpecialtyPayload>()).Data!;

        // The duplicate check must exclude the row being edited.
        var response = await admin.PutAsJsonAsync($"/api/super-admin/specialties/{created.Id}", new
        {
            name,
            description = "Only the description changed."
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_ToggleStatusDoesNotDeleteTheSpecialty()
    {
        var admin = await _scenario.SuperAdminClientAsync();

        var created = (await (await admin.PostAsJsonAsync("/api/super-admin/specialties", new
        {
            name = $"Temporary Specialty {Guid.NewGuid():N}"[..30]
        })).ReadEnvelopeAsync<SpecialtyPayload>()).Data!;

        var deactivate = await admin.PatchAsync($"/api/super-admin/specialties/{created.Id}/toggle-status", null);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        Assert.False((await deactivate.ReadEnvelopeAsync<ToggleSpecialtyPayload>()).Data!.IsActive);

        // Still present in the admin listing, just inactive.
        var listing = await (await admin.GetAsync($"/api/super-admin/specialties?search={created.Name}"))
            .ReadEnvelopeAsync<PagedPayload<SpecialtyPayload>>();

        Assert.Contains(listing.Data!.Items, s => s.Id == created.Id && !s.IsActive);

        var reactivate = await admin.PatchAsync($"/api/super-admin/specialties/{created.Id}/toggle-status", null);
        Assert.True((await reactivate.ReadEnvelopeAsync<ToggleSpecialtyPayload>()).Data!.IsActive);
    }

    [Fact]
    public async Task SuperAdmin_ListingSupportsSearchActiveFilterAndPaging()
    {
        var admin = await _scenario.SuperAdminClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..8];

        for (var i = 0; i < 3; i++)
        {
            await admin.PostAsJsonAsync("/api/super-admin/specialties", new
            {
                name = $"Probe {marker} {i}",
                arabicName = $"فحص {marker} {i}"
            });
        }

        var byEnglish = await (await admin.GetAsync($"/api/super-admin/specialties?search=Probe {marker}"))
            .ReadEnvelopeAsync<PagedPayload<SpecialtyPayload>>();
        Assert.Equal(3, byEnglish.Data!.TotalCount);

        // Arabic search hits the ArabicName column.
        var byArabic = await (await admin.GetAsync($"/api/super-admin/specialties?search=فحص {marker}"))
            .ReadEnvelopeAsync<PagedPayload<SpecialtyPayload>>();
        Assert.Equal(3, byArabic.Data!.TotalCount);

        var firstPage = await (await admin.GetAsync(
                $"/api/super-admin/specialties?search=Probe {marker}&page=1&pageSize=2"))
            .ReadEnvelopeAsync<PagedPayload<SpecialtyPayload>>();

        Assert.Equal(2, firstPage.Data!.Items.Count);
        Assert.Equal(2, firstPage.Data.TotalPages);
        Assert.True(firstPage.Data.HasNextPage);

        var inactiveOnly = await (await admin.GetAsync(
                $"/api/super-admin/specialties?search=Probe {marker}&isActive=false"))
            .ReadEnvelopeAsync<PagedPayload<SpecialtyPayload>>();
        Assert.Equal(0, inactiveOnly.Data!.TotalCount);
    }

    [Fact]
    public async Task SuperAdmin_RejectsAnEmptyOrOverlongName()
    {
        var admin = await _scenario.SuperAdminClientAsync();

        var empty = await admin.PostAsJsonAsync("/api/super-admin/specialties", new { name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        var tooLong = await admin.PostAsJsonAsync(
            "/api/super-admin/specialties",
            new { name = new string('x', 200) });

        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_Update_Returns404ForAnUnknownSpecialty()
    {
        var admin = await _scenario.SuperAdminClientAsync();

        var response = await admin.PutAsJsonAsync(
            $"/api/super-admin/specialties/{Guid.NewGuid()}",
            new { name = "Ghost Specialty" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ------------------------------------------------------------ Authorization

    [Theory]
    [InlineData(AppRoles.Patient)]
    [InlineData(AppRoles.Doctor)]
    [InlineData(AppRoles.Hospital)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task SpecialtyAdministration_IsClosedToEveryNonAdminRole(string role)
    {
        var (client, _) = await _scenario.NewAccountAsync(role, $"specialty-403-{role.ToLowerInvariant()}");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("/api/super-admin/specialties")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("/api/super-admin/specialties", new { name = "Sneaky" })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync($"/api/super-admin/specialties/{Guid.NewGuid()}", new { name = "Sneaky" }))
            .StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PatchAsync($"/api/super-admin/specialties/{Guid.NewGuid()}/toggle-status", null))
            .StatusCode);
    }

    [Fact]
    public async Task SpecialtyAdministration_Returns401ForAnonymousCallers()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/super-admin/specialties")).StatusCode);
    }
}
