using System.Net;
using System.Net.Http.Json;
using Shouldly;
using WiseAuth.Sample.Modules.Products;
using WiseAuth.Sample.Modules.Users;

namespace WiseAuth.Sample.IntegrationTests;

public class UsersAuthorizationTests
{
    [ClassDataSource<SampleAppFactory>(Shared = SharedType.PerAssembly)]
    public required SampleAppFactory Factory { get; init; }

    [Test]
    public async Task Admin_GetUsers_ReturnsSeededUsers()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);

        var response = await client.GetAsync("/api/users");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<UserSummary[]>();
        var userNames = users!.Select(u => u.UserName).ToArray();
        userNames.ShouldContain(SeedUsers.AdminUserName);
        userNames.ShouldContain(SeedUsers.ViewerUserName);
        userNames.ShouldContain(SeedUsers.AuditorUserName);
    }

    [Test]
    public async Task Admin_GetUserById_ReturnsUser()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var viewerId = await GetUserIdAsync(client, SeedUsers.ViewerUserName);

        var response = await client.GetAsync($"/api/users/{viewerId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Admin_GetUserClaims_ReturnsPersonalClaimsOnly()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var auditorId = await GetUserIdAsync(client, SeedUsers.AuditorUserName);

        var response = await client.GetAsync($"/api/users/{auditorId}/claims");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var claims = await response.Content.ReadFromJsonAsync<Dictionary<string, ulong>>();
        // Raw claims, not the role-merged effective permissions: Auditor's Read bit comes from
        // the Viewers role, so only the personal Export claim shows up here.
        claims!["products"].ShouldBe((ulong)ProductPermissions.Export);
    }

    [Test]
    public async Task Admin_GetUserRoles_ReturnsMemberRoles()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var viewerId = await GetUserIdAsync(client, SeedUsers.ViewerUserName);

        var response = await client.GetAsync($"/api/users/{viewerId}/roles");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<UserRolesResponse>();
        roles!.MemberRoleNames.ShouldContain("Viewers");
    }

    [Test]
    public async Task Admin_CreateUser_ReturnsCreated()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var userName = $"it-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/users",
            new CreateUserRequest(userName, "Integration Test User", $"{userName}@wiseauth.sample", "Passw0rd!", Permissions: null));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<UserSummary>();
        created!.UserName.ShouldBe(userName);
    }

    [Test]
    public async Task Admin_UpdateUserClaims_ReturnsOk()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var targetId = await CreateDedicatedUserAsync(client);

        var response = await client.PutAsJsonAsync($"/api/users/{targetId}/claims",
            new UpdateUserClaimsRequest { Permissions = new Dictionary<string, ulong> { ["products"] = (ulong)ProductPermissions.Read } });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var claims = await response.Content.ReadFromJsonAsync<Dictionary<string, ulong>>();
        claims!["products"].ShouldBe((ulong)ProductPermissions.Read);
    }

    [Test]
    public async Task Admin_UpdateUserProfile_ReturnsOk()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var targetId = await CreateDedicatedUserAsync(client);
        var newUserName = $"it-renamed-{Guid.NewGuid():N}";

        var response = await client.PutAsJsonAsync($"/api/users/{targetId}/profile",
            new UpdateUserProfileRequest { UserName = newUserName, DisplayName = "Renamed", Email = $"{newUserName}@wiseauth.sample" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<UserDetail>();
        updated!.UserName.ShouldBe(newUserName);
    }

    [Test]
    public async Task Admin_UpdateUserRoles_ReturnsOk()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var targetId = await CreateDedicatedUserAsync(client);

        var response = await client.PutAsJsonAsync($"/api/users/{targetId}/roles",
            new UpdateUserRolesRequest { RoleNames = ["Viewers"] });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<string[]>();
        roles!.ShouldContain("Viewers");
    }

    [Test]
    public async Task Admin_UpdateOwnClaims_ReturnsBadRequest()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var adminId = await GetUserIdAsync(client, SeedUsers.AdminUserName);

        var response = await client.PutAsJsonAsync($"/api/users/{adminId}/claims",
            new UpdateUserClaimsRequest { Permissions = new Dictionary<string, ulong> { ["products"] = (ulong)ProductPermissions.Read } });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Admin_UpdateOwnRoles_ReturnsBadRequest()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var adminId = await GetUserIdAsync(client, SeedUsers.AdminUserName);

        var response = await client.PutAsJsonAsync($"/api/users/{adminId}/roles",
            new UpdateUserRolesRequest { RoleNames = ["Viewers"] });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    [Arguments(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword, "GET", "/api/users")]
    [Arguments(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword, "GET", "/api/users/placeholder-id")]
    [Arguments(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword, "GET", "/api/users/placeholder-id/claims")]
    [Arguments(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword, "GET", "/api/users/placeholder-id/roles")]
    [Arguments(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword, "POST", "/api/users")]
    [Arguments(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword, "PUT", "/api/users/placeholder-id/claims")]
    [Arguments(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword, "PUT", "/api/users/placeholder-id/profile")]
    [Arguments(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword, "PUT", "/api/users/placeholder-id/roles")]
    [Arguments(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword, "GET", "/api/users")]
    [Arguments(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword, "GET", "/api/users/placeholder-id")]
    [Arguments(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword, "GET", "/api/users/placeholder-id/claims")]
    [Arguments(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword, "GET", "/api/users/placeholder-id/roles")]
    [Arguments(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword, "POST", "/api/users")]
    [Arguments(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword, "PUT", "/api/users/placeholder-id/claims")]
    [Arguments(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword, "PUT", "/api/users/placeholder-id/profile")]
    [Arguments(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword, "PUT", "/api/users/placeholder-id/roles")]
    public async Task NonAdminUser_AnyUsersEndpoint_ReturnsForbidden(string userName, string password, string method, string route)
    {
        // Neither Viewer nor Auditor holds a "users" claim at all (not even a zero-value one),
        // so every Users endpoint - read or write - is forbidden for both.
        var client = await Factory.CreateAuthenticatedClientAsync(userName, password);

        var response = await client.SendRequestAsync(method, route);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<string> GetUserIdAsync(HttpClient adminClient, string userName)
    {
        var users = await adminClient.GetFromJsonAsync<UserSummary[]>("/api/users");
        return users!.First(u => u.UserName == userName).Id;
    }

    // Positive claims/profile/roles-update tests act on a throwaway user created here rather
    // than a seeded one: editing Admin's own claims/roles is blocked by a self-edit business
    // rule regardless of permissions (see Admin_UpdateOwnClaims_ReturnsBadRequest below).
    private static async Task<string> CreateDedicatedUserAsync(HttpClient adminClient)
    {
        var userName = $"it-{Guid.NewGuid():N}";
        var response = await adminClient.PostAsJsonAsync("/api/users",
            new CreateUserRequest(userName, "Dedicated Test User", $"{userName}@wiseauth.sample", "Passw0rd!", Permissions: null));
        var created = await response.Content.ReadFromJsonAsync<UserSummary>();
        return created!.Id;
    }
}
