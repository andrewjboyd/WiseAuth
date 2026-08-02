using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WiseAuth.Sample.Modules.Auth;

namespace WiseAuth.Sample.IntegrationTests;

internal static class HttpClientAuthExtensions
{
    // Program.cs marks the auth cookie Secure-only. WebApplicationFactory's default client
    // uses an http:// BaseAddress, and System.Net.CookieContainer silently drops Secure
    // cookies on requests whose URI scheme isn't https - so without this, the cookie from
    // /api/auth/login would never be replayed and every following request would 401, even
    // though TestServer itself never does a real TLS handshake either way.
    public static HttpClient CreateHttpsClient(this SampleAppFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    private static async Task LoginAsync(this HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(userName, password));
        response.EnsureSuccessStatusCode();
    }

    // Each call gets its own HttpClient/cookie container, so tests running in parallel
    // never share a login session even though they share one SampleAppFactory/TestServer.
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this SampleAppFactory factory, string userName, string password)
    {
        var client = factory.CreateHttpsClient();
        await client.LoginAsync(userName, password);
        return client;
    }
}
