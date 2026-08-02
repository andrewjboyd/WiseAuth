using System.Net;
using Shouldly;

namespace WiseAuth.Sample.IntegrationTests;

public class UnauthenticatedAccessTests
{
    [ClassDataSource<SampleAppFactory>(Shared = SharedType.PerAssembly)]
    public required SampleAppFactory Factory { get; init; }

    [Test]
    [Arguments("GET", "/api/auth/me")]
    [Arguments("GET", "/api/products")]
    [Arguments("GET", "/api/products/export")]
    [Arguments("GET", "/api/products/1")]
    [Arguments("POST", "/api/products")]
    [Arguments("PUT", "/api/products/1")]
    [Arguments("DELETE", "/api/products/1")]
    [Arguments("GET", "/api/users")]
    [Arguments("GET", "/api/users/placeholder-id")]
    [Arguments("GET", "/api/users/placeholder-id/claims")]
    [Arguments("GET", "/api/users/placeholder-id/roles")]
    [Arguments("POST", "/api/users")]
    [Arguments("PUT", "/api/users/placeholder-id/claims")]
    [Arguments("PUT", "/api/users/placeholder-id/profile")]
    [Arguments("PUT", "/api/users/placeholder-id/roles")]
    [Arguments("GET", "/api/roles")]
    [Arguments("GET", "/api/roles/placeholder-id")]
    [Arguments("POST", "/api/roles")]
    [Arguments("PUT", "/api/roles/placeholder-id/permissions")]
    public async Task ProtectedEndpoint_WithoutAuthentication_ReturnsUnauthorized(string method, string route)
    {
        var client = Factory.CreateClient();

        var response = await client.SendRequestAsync(method, route);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
