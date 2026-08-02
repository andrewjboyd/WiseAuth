using System.Net;
using System.Net.Http.Json;
using Shouldly;
using WiseAuth.Sample.Modules.Auth;
using WiseAuth.Sample.Modules.Products;
using WiseAuth.Sample.Modules.Roles;
using WiseAuth.Sample.Modules.Users;

namespace WiseAuth.Sample.IntegrationTests;

public class AuthEndpointTests
{
    [ClassDataSource<SampleAppFactory>(Shared = SharedType.PerAssembly)]
    public required SampleAppFactory Factory { get; init; }

    [Test]
    public async Task Login_Admin_ReturnsFullPermissionsFromRole()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(SeedUsers.AdminUserName, SeedUsers.AdminPassword));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.ShouldNotBeNull();
        // All via the Admins role.
        auth.Permissions["products"].ShouldBe((ulong)(ProductPermissions.Read | ProductPermissions.Write | ProductPermissions.Delete | ProductPermissions.Export));
        auth.Permissions["users"].ShouldBe((ulong)(UserPermissions.View | UserPermissions.Manage));
        auth.Permissions["roles"].ShouldBe((ulong)(RolePermissions.View | RolePermissions.Manage));
    }

    [Test]
    public async Task Login_Viewer_ReturnsReadOnlyProductsPermissionFromRole()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(SeedUsers.ViewerUserName, SeedUsers.ViewerPassword));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.ShouldNotBeNull();
        auth.Permissions["products"].ShouldBe((ulong)ProductPermissions.Read); // Via the Viewers role
        auth.Permissions.ShouldNotContainKey("users");
        auth.Permissions.ShouldNotContainKey("roles");
    }

    [Test]
    public async Task Login_Auditor_ReturnsRoleReadPlusPersonalExportClaim()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(SeedUsers.AuditorUserName, SeedUsers.AuditorPassword));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.ShouldNotBeNull();
        // Read comes from the Viewers role, Export from the personal claim.
        auth.Permissions["products"].ShouldBe((ulong)(ProductPermissions.Read | ProductPermissions.Export));
        auth.Permissions.ShouldNotContainKey("users");
        auth.Permissions.ShouldNotContainKey("roles");
    }

    [Test]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(SeedUsers.AdminUserName, "not-the-password"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_UnknownUserName_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("does-not-exist", "whatever"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Me_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Me_AfterLogin_ReturnsAuthenticatedUser()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.ShouldNotBeNull();
        auth.DisplayName.ShouldBe("Admin");
    }

    [Test]
    public async Task Logout_WithoutAuthentication_ReturnsOk()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Logout_ThenMe_ReturnsUnauthorized()
    {
        var client = await Factory.CreateAuthenticatedClientAsync(SeedUsers.AdminUserName, SeedUsers.AdminPassword);

        var logoutResponse = await client.PostAsync("/api/auth/logout", content: null);
        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var meResponse = await client.GetAsync("/api/auth/me");

        meResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
