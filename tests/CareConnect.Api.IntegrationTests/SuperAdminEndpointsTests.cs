using System.Net;
using System.Net.Http.Json;
using CareConnect.Domain.Constants;

namespace CareConnect.Api.IntegrationTests;

[Collection(nameof(ApiCollection))]
public class SuperAdminEndpointsTests
{
    private readonly CareConnectApiFactory _factory;

    public SuperAdminEndpointsTests(CareConnectApiFactory factory) => _factory = factory;

    [Fact]
    public async Task SeededSuperAdmin_CanSignInAndListUsers()
    {
        var client = _factory.CreateClient();

        var auth = await AuthEndpointsTests.LoginAsync(
            client, CareConnectApiFactory.SuperAdminEmail, CareConnectApiFactory.SuperAdminPassword);

        Assert.Equal(AppRoles.SuperAdmin, auth.User.Role);

        client.UseBearer(auth.AccessToken);

        // Searched by email rather than relying on default (unpaged, newest-first) ordering:
        // the suite creates hundreds of accounts across other test classes, so the seeded
        // SuperAdmin - created once, before anything else - would otherwise fall off the
        // first page.
        var response = await client.GetAsync(
            $"/api/super-admin/users?search={Uri.EscapeDataString(CareConnectApiFactory.SuperAdminEmail)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.ReadEnvelopeAsync<PagedPayload<UserPayload>>();
        Assert.True(envelope.Success);
        Assert.Contains(envelope.Data!.Items, u => u.Email == CareConnectApiFactory.SuperAdminEmail);
    }

    [Fact]
    public async Task UserList_NeverLeaksPasswordHashesOrTokens()
    {
        var client = await SignInAsSuperAdminAsync();

        var response = await client.GetAsync("/api/super-admin/users?pageSize=100");
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("concurrencyStamp", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UserList_SupportsSearchRoleFilterAndPagination()
    {
        var anonymous = _factory.CreateClient();

        var marker = Guid.NewGuid().ToString("N")[..8];
        var emails = new List<string>();

        for (var i = 0; i < 3; i++)
        {
            var email = $"filter.{marker}.{i}@careconnect.test";
            emails.Add(email);

            await anonymous.PostAsJsonAsync("/api/auth/register", new
            {
                fullName = $"Filter Probe {marker} {i}",
                email,
                password = "StrongPass123!",
                confirmPassword = "StrongPass123!",
                role = AppRoles.Doctor
            });
        }

        var client = await SignInAsSuperAdminAsync();

        // Search by name.
        var byName = await (await client.GetAsync($"/api/super-admin/users?search=Filter Probe {marker}"))
            .ReadEnvelopeAsync<PagedPayload<UserPayload>>();
        Assert.Equal(3, byName.Data!.TotalCount);

        // Search by email.
        var byEmail = await (await client.GetAsync($"/api/super-admin/users?search=filter.{marker}"))
            .ReadEnvelopeAsync<PagedPayload<UserPayload>>();
        Assert.Equal(3, byEmail.Data!.TotalCount);
        Assert.All(byEmail.Data.Items, u => Assert.Equal(AppRoles.Doctor, u.Role));

        // Role filter that excludes them.
        var wrongRole = await (await client.GetAsync(
                $"/api/super-admin/users?search=filter.{marker}&role={AppRoles.Hospital}"))
            .ReadEnvelopeAsync<PagedPayload<UserPayload>>();
        Assert.Equal(0, wrongRole.Data!.TotalCount);

        // Pagination.
        var firstPage = await (await client.GetAsync(
                $"/api/super-admin/users?search=filter.{marker}&page=1&pageSize=2"))
            .ReadEnvelopeAsync<PagedPayload<UserPayload>>();

        Assert.Equal(2, firstPage.Data!.Items.Count);
        Assert.Equal(2, firstPage.Data.TotalPages);
        Assert.True(firstPage.Data.HasNextPage);
        Assert.False(firstPage.Data.HasPreviousPage);

        var secondPage = await (await client.GetAsync(
                $"/api/super-admin/users?search=filter.{marker}&page=2&pageSize=2"))
            .ReadEnvelopeAsync<PagedPayload<UserPayload>>();

        Assert.Single(secondPage.Data!.Items);
        Assert.True(secondPage.Data.HasPreviousPage);

        // Active filter.
        var activeOnly = await (await client.GetAsync(
                $"/api/super-admin/users?search=filter.{marker}&isActive=true"))
            .ReadEnvelopeAsync<PagedPayload<UserPayload>>();
        Assert.Equal(3, activeOnly.Data!.TotalCount);

        Assert.Equal(3, emails.Count);
    }

    [Fact]
    public async Task UserList_RejectsAnUnknownRoleFilter()
    {
        var client = await SignInAsSuperAdminAsync();

        var response = await client.GetAsync("/api/super-admin/users?role=Wizard");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ------------------------------------------------------------- Authorization

    [Fact]
    public async Task SuperAdminEndpoints_Return401ForAnonymousCallers()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/super-admin/users")).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PatchAsync("/api/super-admin/users/anything/toggle-status", null)).StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.Patient)]
    [InlineData(AppRoles.Doctor)]
    [InlineData(AppRoles.Hospital)]
    [InlineData(AppRoles.MedicalServiceProvider)]
    public async Task SuperAdminEndpoints_Return403ForEveryNonAdminRole(string role)
    {
        var client = _factory.CreateClient();
        var email = TestHttp.UniqueEmail($"forbidden-{role.ToLowerInvariant()}");

        await AuthEndpointsTests.RegisterAsync(client, email, role);
        var auth = await AuthEndpointsTests.LoginAsync(client, email);

        client.UseBearer(auth.AccessToken);

        var list = await client.GetAsync("/api/super-admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);

        var toggle = await client.PatchAsync($"/api/super-admin/users/{auth.User.Id}/toggle-status", null);
        Assert.Equal(HttpStatusCode.Forbidden, toggle.StatusCode);
    }

    // ------------------------------------------------------------- Toggle status

    [Fact]
    public async Task ToggleStatus_DeactivatedUserCannotSignInAndReactivationRestoresAccess()
    {
        var anonymous = _factory.CreateClient();
        var email = TestHttp.UniqueEmail("toggle");

        await AuthEndpointsTests.RegisterAsync(anonymous, email, AppRoles.Patient);
        var beforeDeactivation = await AuthEndpointsTests.LoginAsync(anonymous, email);

        var admin = await SignInAsSuperAdminAsync();

        var deactivate = await admin.PatchAsync(
            $"/api/super-admin/users/{beforeDeactivation.User.Id}/toggle-status", null);

        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var deactivated = await deactivate.ReadEnvelopeAsync<ToggleStatusPayload>();
        Assert.False(deactivated.Data!.IsActive);

        // Sign-in is blocked with a clear 403, not a generic credential error.
        var blocked = await anonymous.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "StrongPass123!"
        });

        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);

        var blockedEnvelope = await blocked.ReadEnvelopeAsync<object>();
        Assert.Contains("deactivated", blockedEnvelope.Message, StringComparison.OrdinalIgnoreCase);

        // Existing refresh tokens were revoked at the same time.
        var refresh = await anonymous.PostAsJsonAsync(
            "/api/auth/refresh-token", new { refreshToken = beforeDeactivation.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        // Reactivating restores access.
        var reactivate = await admin.PatchAsync(
            $"/api/super-admin/users/{beforeDeactivation.User.Id}/toggle-status", null);

        var reactivated = await reactivate.ReadEnvelopeAsync<ToggleStatusPayload>();
        Assert.True(reactivated.Data!.IsActive);

        var allowed = await anonymous.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "StrongPass123!"
        });

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task ToggleStatus_RefusesToDeactivateTheAdminsOwnAccount()
    {
        var client = _factory.CreateClient();

        var auth = await AuthEndpointsTests.LoginAsync(
            client, CareConnectApiFactory.SuperAdminEmail, CareConnectApiFactory.SuperAdminPassword);

        client.UseBearer(auth.AccessToken);

        var response = await client.PatchAsync(
            $"/api/super-admin/users/{auth.User.Id}/toggle-status", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ToggleStatus_Returns404ForAnUnknownUser()
    {
        var client = await SignInAsSuperAdminAsync();

        var response = await client.PatchAsync(
            $"/api/super-admin/users/{Guid.NewGuid()}/toggle-status", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpClient> SignInAsSuperAdminAsync()
    {
        var client = _factory.CreateClient();

        var auth = await AuthEndpointsTests.LoginAsync(
            client, CareConnectApiFactory.SuperAdminEmail, CareConnectApiFactory.SuperAdminPassword);

        client.UseBearer(auth.AccessToken);
        return client;
    }
}
