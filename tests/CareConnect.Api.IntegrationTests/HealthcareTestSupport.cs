using System.Net.Http.Json;
using CareConnect.Domain.Constants;

namespace CareConnect.Api.IntegrationTests;

// --------------------------------------------------------------- Payload shapes

public class SpecialtyOptionPayload
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ArabicName { get; set; }
}

public class SpecialtyPayload : SpecialtyOptionPayload
{
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DoctorCount { get; set; }
    public int HospitalCount { get; set; }
}

public class ToggleSpecialtyPayload
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class DoctorProfilePayload
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public SpecialtyOptionPayload? Specialty { get; set; }
    public string? LicenseNumber { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Biography { get; set; }
    public decimal? ConsultationPrice { get; set; }
    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsProfileCompleted { get; set; }
    public List<string> MissingFields { get; set; } = [];
}

public class HospitalProfilePayload
{
    public Guid Id { get; set; }
    public string? HospitalName { get; set; }
    public string? Address { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? OpeningTime { get; set; }
    public string? ClosingTime { get; set; }
    public bool IsProfileCompleted { get; set; }
    public List<string> MissingFields { get; set; } = [];
    public List<SpecialtyOptionPayload> Specialties { get; set; } = [];
}

public class DoctorRequestPayload
{
    public Guid Id { get; set; }
    public Guid HospitalProfileId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public bool IsPrimary { get; set; }
}

public class HospitalRequestPayload
{
    public Guid Id { get; set; }
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public SpecialtyOptionPayload? Specialty { get; set; }
    public string? LicenseNumber { get; set; }
    public int? YearsOfExperience { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime RequestedAt { get; set; }
}

public class HospitalDoctorPayload
{
    public Guid AffiliationId { get; set; }
    public Guid DoctorProfileId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public SpecialtyOptionPayload? Specialty { get; set; }
    public bool IsPrimary { get; set; }
}

public class AffiliatedHospitalPayload
{
    public Guid Id { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class HospitalDirectoryPayload
{
    public Guid Id { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? PhoneNumber { get; set; }
    public List<SpecialtyOptionPayload> Specialties { get; set; } = [];
    public int NumberOfApprovedDoctors { get; set; }
    public List<DirectoryDoctorPayload> Doctors { get; set; } = [];
}

public class DirectoryDoctorPayload
{
    public Guid DoctorProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public SpecialtyOptionPayload? Specialty { get; set; }
}

public class DoctorDirectoryPayload
{
    public Guid DoctorProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public SpecialtyOptionPayload? Specialty { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? LicenseNumber { get; set; }
    public List<DirectoryHospitalPayload> Hospitals { get; set; } = [];
}

public class DirectoryHospitalPayload
{
    public Guid Id { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

// -------------------------------------------------------------------- Helpers

/// <summary>
/// Builds fully set-up doctors and hospitals so each test can start from the state it
/// actually wants to exercise rather than repeating six requests of scaffolding.
/// </summary>
public class HealthcareScenario
{
    private readonly CareConnectApiFactory _factory;

    public HealthcareScenario(CareConnectApiFactory factory) => _factory = factory;

    /// <summary>Signs in as the seeded SuperAdmin.</summary>
    public async Task<HttpClient> SuperAdminClientAsync()
    {
        var client = _factory.CreateClient();

        var auth = await AuthEndpointsTests.LoginAsync(
            client, CareConnectApiFactory.SuperAdminEmail, CareConnectApiFactory.SuperAdminPassword);

        client.UseBearer(auth.AccessToken);
        return client;
    }

    /// <summary>Registers and signs in a new account in the given role.</summary>
    public async Task<(HttpClient Client, AuthPayload Auth)> NewAccountAsync(string role, string prefix)
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail(prefix);

        var register = await AuthEndpointsTests.RegisterAsync(client, email, role);
        register.EnsureSuccessStatusCode();

        var auth = await AuthEndpointsTests.LoginAsync(client, email);
        client.UseBearer(auth.AccessToken);

        return (client, auth);
    }

    /// <summary>Any active specialty, used when a test just needs "a valid specialty".</summary>
    public async Task<SpecialtyOptionPayload> AnySpecialtyAsync(HttpClient client, string name = "Cardiology")
    {
        var envelope = await (await client.GetAsync("/api/specialties"))
            .ReadEnvelopeAsync<List<SpecialtyOptionPayload>>();

        var match = envelope.Data!.FirstOrDefault(s => s.Name == name) ?? envelope.Data!.First();
        return match;
    }

    /// <summary>A doctor whose profile passes every completion rule.</summary>
    public async Task<(HttpClient Client, AuthPayload Auth, DoctorProfilePayload Profile)>
        CompletedDoctorAsync(Guid specialtyId, string prefix = "doctor", string? city = null)
    {
        var (client, auth) = await NewAccountAsync(AppRoles.Doctor, prefix);

        var response = await client.PutAsJsonAsync("/api/doctor/profile", new
        {
            fullName = auth.User.FullName,
            phoneNumber = (string?)null,
            specialtyId,
            licenseNumber = $"LIC-{Guid.NewGuid():N}"[..12],
            yearsOfExperience = 7,
            biography = "Experienced clinician taking part in the CareConnect network.",
            consultationPrice = 350m,
            address = "12 Nile Street",
            governorate = "Cairo",
            city = city ?? "Nasr City",
            profileImageUrl = (string?)null
        });

        response.EnsureSuccessStatusCode();
        var profile = (await response.ReadEnvelopeAsync<DoctorProfilePayload>()).Data!;

        return (client, auth, profile);
    }

    /// <summary>A hospital whose profile is complete and which offers the given specialties.</summary>
    public async Task<(HttpClient Client, AuthPayload Auth, HospitalProfilePayload Profile)>
        CompletedHospitalAsync(IEnumerable<Guid> specialtyIds, string prefix = "hospital", string? city = null)
    {
        var (client, auth) = await NewAccountAsync(AppRoles.Hospital, prefix);

        var profileResponse = await client.PutAsJsonAsync("/api/hospital/profile", new
        {
            hospitalName = $"CareConnect Test Hospital {Guid.NewGuid():N}"[..40],
            address = "5 El Tahrir Square",
            governorate = "Cairo",
            city = city ?? "Downtown",
            phoneNumber = $"+2027{Random.Shared.Next(1_000_000, 9_999_999)}",
            description = "A teaching hospital used by the CareConnect integration tests.",
            openingTime = "08:00",
            closingTime = "22:00"
        });

        profileResponse.EnsureSuccessStatusCode();

        var specialtiesResponse = await client.PutAsJsonAsync(
            "/api/hospital/profile/specialties",
            new { specialtyIds = specialtyIds.ToArray() });

        specialtiesResponse.EnsureSuccessStatusCode();
        var profile = (await specialtiesResponse.ReadEnvelopeAsync<HospitalProfilePayload>()).Data!;

        return (client, auth, profile);
    }
}
