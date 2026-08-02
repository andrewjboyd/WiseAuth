using System.Net;
using System.Net.Http.Json;
using Shouldly;
using WiseAuth.Sample.Modules.Products;
using WiseAuth.Sample.Modules.Roles;

namespace WiseAuth.Sample.IntegrationTests;

public class RolesAuthorizationTests
{
    [ClassDataSource<SampleAppFactory>(Shared = SharedType.PerAssembly)]
    public required SampleAppFactory Factory { get; init; }

    [Test]
    public async Task Admin_GetRoles_ReturnsSeededRoles()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);

        var response = await client.GetAsync("/api/roles");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<RoleSummary[]>();
        var roleNames = roles!.Select(r => r.Name).ToArray();
        roleNames.ShouldContain("Admins");
        roleNames.ShouldContain("Viewers");
    }

    [Test]
    public async Task Admin_GetRoleById_ReturnsRole()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var viewersId = await GetRoleIdAsync(client, "Viewers");

        var response = await client.GetAsync($"/api/roles/{viewersId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Admin_CreateRole_ReturnsCreated()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var roleName = $"it-role-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/roles",
            new CreateRoleRequest(roleName, new Dictionary<string, ulong> { ["products"] = (ulong)ProductPermissions.Read }));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<RoleSummary>();
        created!.Name.ShouldBe(roleName);
        created.Permissions["products"].ShouldBe((ulong)ProductPermissions.Read);
    }

    [Test]
    public async Task Admin_UpdateRolePermissions_ReturnsOk()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var roleId = await CreateDedicatedRoleAsync(client);

        var response = await client.PutAsJsonAsync($"/api/roles/{roleId}/permissions",
            new UpdateRolePermissionsRequest(new Dictionary<string, ulong> { ["products"] = (ulong)(ProductPermissions.Read | ProductPermissions.Export) }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<RoleSummary>();
        updated!.Permissions["products"].ShouldBe((ulong)(ProductPermissions.Read | ProductPermissions.Export));
    }

    [Test]
    public async Task Admin_UpdatePermissionsOfOwnRole_ReturnsBadRequest()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);
        var adminsRoleId = await GetRoleIdAsync(client, "Admins");

        var response = await client.PutAsJsonAsync($"/api/roles/{adminsRoleId}/permissions",
            new UpdateRolePermissionsRequest(new Dictionary<string, ulong> { ["products"] = (ulong)ProductPermissions.Read }));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    [Arguments(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword, "GET", "/api/roles")]
    [Arguments(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword, "GET", "/api/roles/placeholder-id")]
    [Arguments(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword, "POST", "/api/roles")]
    [Arguments(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword, "PUT", "/api/roles/placeholder-id/permissions")]
    [Arguments(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword, "GET", "/api/roles")]
    [Arguments(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword, "GET", "/api/roles/placeholder-id")]
    [Arguments(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword, "POST", "/api/roles")]
    [Arguments(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword, "PUT", "/api/roles/placeholder-id/permissions")]
    public async Task NonAdminUser_AnyRolesEndpoint_ReturnsForbidden(string userName, string password, string method, string route)
    {
        // Neither Viewer nor Auditor holds a "roles" claim at all, so every Roles endpoint -
        // read or write - is forbidden for both.
        var client = await Factory.CreateAuthenticatedClientAsync(userName, password);

        var response = await client.SendRequestAsync(method, route);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<string> GetRoleIdAsync(HttpClient adminClient, string roleName)
    {
        var roles = await adminClient.GetFromJsonAsync<RoleSummary[]>("/api/roles");
        return roles!.First(r => r.Name == roleName).Id;
    }

    // Positive permission-update tests act on a throwaway role created here rather than a
    // seeded one: Admin belongs to "Admins", and editing the permissions of a role you belong
    // to is blocked by a self-lockout business rule (see Admin_UpdatePermissionsOfOwnRole_ReturnsBadRequest).
    private static async Task<string> CreateDedicatedRoleAsync(HttpClient adminClient)
    {
        var roleName = $"it-role-{Guid.NewGuid():N}";
        var response = await adminClient.PostAsJsonAsync("/api/roles", new CreateRoleRequest(roleName, Permissions: null));
        var created = await response.Content.ReadFromJsonAsync<RoleSummary>();
        return created!.Id;
    }
}
