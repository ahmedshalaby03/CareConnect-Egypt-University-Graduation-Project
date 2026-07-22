using System.Net;
using System.Net.Http.Json;
using CareConnect.Domain.Constants;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CareConnect.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class AuthEndpointsTests
{
    private readonly CareConnectApiFactory _factory;

    public AuthEndpointsTests(CareConnectApiFactory factory) => _factory = factory;

    // ------------------------------------------------------------------ Register

    [Theory]
    [InlineData(AppRoles.Patient)]
    [InlineData(AppRoles.Doctor)]
    [InlineData(AppRoles.Hospital)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task Register_CreatesAccount_ForEveryPublicRole(string role)
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail(role.ToLowerInvariant());

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = $"{role} Test User",
            email,
            phoneNumber = (string?)null,
            password = "StrongPass123!",
            confirmPassword = "StrongPass123!",
            role
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var envelope = await response.ReadEnvelopeAsync<RegisterPayload>();
        Assert.True(envelope.Success);
        Assert.Equal("User registered successfully.", envelope.Message);
        Assert.Equal(role, envelope.Data!.Role);
        Assert.Equal(email, envelope.Data.Email);
        Assert.NotEmpty(envelope.Data.UserId);
    }

    [Fact]
    public async Task Register_RejectsSuperAdminRole()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Would Be Admin",
            email = TestHttp.UniqueEmail("escalation"),
            password = "StrongPass123!",
            confirmPassword = "StrongPass123!",
            role = AppRoles.SuperAdmin
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var envelope = await response.ReadEnvelopeAsync<object>();
        Assert.False(envelope.Success);
        Assert.Contains(envelope.Errors!, e => e.Contains("Patient", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Register_RejectsUnknownRole()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Made Up Role",
            email = TestHttp.UniqueEmail("unknown-role"),
            password = "StrongPass123!",
            confirmPassword = "StrongPass123!",
            role = "Administrator"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_RejectsDuplicateEmail()
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail("duplicate");

        var first = await RegisterAsync(client, email, AppRoles.Patient);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await RegisterAsync(client, email, AppRoles.Doctor);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var envelope = await second.ReadEnvelopeAsync<object>();
        Assert.Equal("An account with this email already exists.", envelope.Message);
    }

    [Fact]
    public async Task Register_RejectsDuplicatePhoneNumber()
    {
        var client = _factory.CreateClient();
        var phone = $"+2010{Random.Shared.Next(10_000_000, 99_999_999)}";

        var first = await RegisterAsync(client, TestHttp.UniqueEmail("phone-a"), AppRoles.Patient, phone);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await RegisterAsync(client, TestHttp.UniqueEmail("phone-b"), AppRoles.Patient, phone);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var envelope = await second.ReadEnvelopeAsync<object>();
        Assert.Equal("An account with this phone number already exists.", envelope.Message);
    }

    [Fact]
    public async Task Register_RejectsMismatchedConfirmationAndWeakPassword()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "We",
            email = "not-an-email",
            password = "weak",
            confirmPassword = "different",
            role = AppRoles.Patient
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var envelope = await response.ReadEnvelopeAsync<object>();
        Assert.False(envelope.Success);
        Assert.Contains(envelope.Errors!, e => e.Contains("valid email", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(envelope.Errors!, e => e.Contains("do not match", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(envelope.Errors!, e => e.Contains("at least 8 characters", StringComparison.OrdinalIgnoreCase));
    }

    // --------------------------------------------------------------------- Login

    [Fact]
    public async Task Login_ReturnsTokensAndPutsTheExpectedClaimsInTheJwt()
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail("login");

        await RegisterAsync(client, email, AppRoles.Doctor);
        var auth = await LoginAsync(client, email);

        Assert.NotEmpty(auth.AccessToken);
        Assert.NotEmpty(auth.RefreshToken);
        Assert.Equal(AppRoles.Doctor, auth.User.Role);
        Assert.True(auth.AccessTokenExpiresAt > DateTime.UtcNow);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(auth.AccessToken);

        Assert.Equal(auth.User.Id, jwt.GetClaim(AppClaimTypes.UserId).Value);
        Assert.Equal(email, jwt.GetClaim(AppClaimTypes.Email).Value);
        Assert.Equal(AppRoles.Doctor, jwt.GetClaim(AppClaimTypes.Role).Value);
        Assert.Equal($"{AppRoles.Doctor} Test User", jwt.GetClaim(AppClaimTypes.FullName).Value);
    }

    [Fact]
    public async Task Login_RejectsWrongPassword()
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail("wrong-password");
        await RegisterAsync(client, email, AppRoles.Patient);

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "NotTheRightPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var envelope = await response.ReadEnvelopeAsync<object>();
        Assert.Equal("Invalid email or password.", envelope.Message);
    }

    [Fact]
    public async Task Login_RejectsUnknownEmailWithTheSameMessageAsAWrongPassword()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestHttp.UniqueEmail("ghost"),
            password = "StrongPass123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var envelope = await response.ReadEnvelopeAsync<object>();
        Assert.Equal("Invalid email or password.", envelope.Message);
    }

    // ----------------------------------------------------------------------- /me

    [Fact]
    public async Task Me_ReturnsTheAuthenticatedUser()
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail("me");

        await RegisterAsync(client, email, AppRoles.Hospital);
        var auth = await LoginAsync(client, email);

        client.UseBearer(auth.AccessToken);
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.ReadEnvelopeAsync<UserPayload>();
        Assert.Equal(email, envelope.Data!.Email);
        Assert.Equal(AppRoles.Hospital, envelope.Data.Role);
        Assert.True(envelope.Data.IsActive);
        Assert.NotNull(envelope.Data.LastLoginAt);
    }

    [Fact]
    public async Task Me_Returns401WithoutAToken()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // The envelope matters: the Angular interceptor reads this message.
        var envelope = await response.ReadEnvelopeAsync<object>();
        Assert.False(envelope.Success);
        Assert.NotEmpty(envelope.Message);
    }

    [Fact]
    public async Task Me_Returns401WithAGarbageToken()
    {
        var client = _factory.CreateClient();
        client.UseBearer("this.is.not-a-jwt");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ------------------------------------------------------------- Refresh token

    [Fact]
    public async Task RefreshToken_IssuesANewPairAndRotatesTheOldOneOut()
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail("refresh");

        await RegisterAsync(client, email, AppRoles.Patient);
        var original = await LoginAsync(client, email);

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = original.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshed = (await refreshResponse.ReadEnvelopeAsync<AuthPayload>()).Data!;
        Assert.NotEqual(original.RefreshToken, refreshed.RefreshToken);
        Assert.NotEmpty(refreshed.AccessToken);

        // The new access token works.
        client.UseBearer(refreshed.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);

        // The rotated-out token does not.
        var reuse = await client.PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = original.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_ReuseOfARevokedTokenKillsTheWholeFamily()
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail("reuse");

        await RegisterAsync(client, email, AppRoles.Patient);
        var first = await LoginAsync(client, email);

        var secondEnvelope = await (await client.PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = first.RefreshToken }))
            .ReadEnvelopeAsync<AuthPayload>();

        var second = secondEnvelope.Data!;

        // Replaying the stolen first token must invalidate the attacker's chain too.
        await client.PostAsJsonAsync("/api/auth/refresh-token", new { refreshToken = first.RefreshToken });

        var afterBreach = await client.PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = second.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, afterBreach.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_RejectsAnUnknownToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = "made-up-token-value" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------- Revoke token

    [Fact]
    public async Task RevokeToken_InvalidatesTheCallersOwnToken()
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail("revoke");

        await RegisterAsync(client, email, AppRoles.Patient);
        var auth = await LoginAsync(client, email);

        client.UseBearer(auth.AccessToken);

        var revoke = await client.PostAsJsonAsync(
            "/api/auth/revoke-token", new { refreshToken = auth.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        var afterRevoke = await client.PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = auth.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, afterRevoke.StatusCode);
    }

    [Fact]
    public async Task RevokeToken_CannotRevokeSomebodyElsesToken()
    {
        var client = _factory.CreateClient();

        var victimEmail = TestHttp.UniqueEmail("victim");
        await RegisterAsync(client, victimEmail, AppRoles.Patient);
        var victim = await LoginAsync(client, victimEmail);

        var attackerEmail = TestHttp.UniqueEmail("attacker");
        await RegisterAsync(client, attackerEmail, AppRoles.Patient);
        var attacker = await LoginAsync(client, attackerEmail);

        client.UseBearer(attacker.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/api/auth/revoke-token", new { refreshToken = victim.RefreshToken });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // The victim's session is untouched.
        var stillWorks = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = victim.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, stillWorks.StatusCode);
    }

    // ------------------------------------------------------------ Change password

    [Fact]
    public async Task ChangePassword_SwapsTheCredentialsAndEndsExistingSessions()
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail("change-password");

        await RegisterAsync(client, email, AppRoles.Patient);
        var auth = await LoginAsync(client, email);

        client.UseBearer(auth.AccessToken);

        var change = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "StrongPass123!",
            newPassword = "EvenStronger456!",
            confirmNewPassword = "EvenStronger456!"
        });

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        var fresh = _factory.CreateClient();

        var oldPassword = await fresh.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "StrongPass123!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);

        var newPassword = await fresh.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "EvenStronger456!"
        });
        Assert.Equal(HttpStatusCode.OK, newPassword.StatusCode);

        // Refresh tokens issued before the change are dead.
        var oldRefresh = await fresh.PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = auth.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefresh.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_RejectsAWrongCurrentPassword()
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail("wrong-current");

        await RegisterAsync(client, email, AppRoles.Patient);
        var auth = await LoginAsync(client, email);
        client.UseBearer(auth.AccessToken);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "NotMyPassword123!",
            newPassword = "EvenStronger456!",
            confirmNewPassword = "EvenStronger456!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------------------------------------------------------------------- Logout

    [Fact]
    public async Task Logout_WithoutABodyRevokesEverySession()
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail("logout-all");

        await RegisterAsync(client, email, AppRoles.Patient);

        var sessionOne = await LoginAsync(client, email);
        var sessionTwo = await LoginAsync(client, email);

        client.UseBearer(sessionTwo.AccessToken);
        var logout = await client.PostAsJsonAsync("/api/auth/logout", new { });
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var fresh = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = sessionOne.RefreshToken })).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = sessionTwo.RefreshToken })).StatusCode);
    }

    [Fact]
    public async Task Logout_WithARefreshTokenOnlyEndsThatSession()
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail("logout-one");

        await RegisterAsync(client, email, AppRoles.Patient);

        var sessionOne = await LoginAsync(client, email);
        var sessionTwo = await LoginAsync(client, email);

        client.UseBearer(sessionTwo.AccessToken);
        await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = sessionTwo.RefreshToken });

        var fresh = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await fresh.PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = sessionTwo.RefreshToken })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await fresh.PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = sessionOne.RefreshToken })).StatusCode);
    }

    [Fact]
    public async Task Logout_Requires401WithoutAToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/logout", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ------------------------------------------------------------------- Helpers

    internal static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string email,
        string role,
        string? phoneNumber = null,
        string password = "StrongPass123!") =>
        client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = $"{role} Test User",
            email,
            phoneNumber,
            password,
            confirmPassword = password,
            role
        });

    internal static async Task<AuthPayload> LoginAsync(
        HttpClient client,
        string email,
        string password = "StrongPass123!")
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var envelope = await response.ReadEnvelopeAsync<AuthPayload>();
        return envelope.Data!;
    }
}
