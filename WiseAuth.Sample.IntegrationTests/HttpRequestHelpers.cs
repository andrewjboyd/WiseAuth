using System.Net.Http.Json;

namespace WiseAuth.Sample.IntegrationTests;

internal static class HttpRequestHelpers
{
    // Authorization runs before model binding, so a forbidden/unauthorized request never
    // needs a realistic body - an empty JSON object is enough to satisfy POST/PUT requests.
    public static Task<HttpResponseMessage> SendRequestAsync(this HttpClient client, string method, string route)
    {
        var httpMethod = new HttpMethod(method);
        var request = new HttpRequestMessage(httpMethod, route);
        if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
        {
            request.Content = JsonContent.Create(new { });
        }

        return client.SendAsync(request);
    }

    // Shared by the Products/Roles/Users authorization tests, which all need to look up the
    // id of a specific seeded or freshly created record before exercising a by-id endpoint.
    public static async Task<TId> GetIdAsync<TResponse, TId>(this HttpClient client, string route, Func<TResponse, bool> predicate, Func<TResponse, TId> idSelector)
    {
        var items = await client.GetFromJsonAsync<TResponse[]>(route);
        return idSelector(items!.First(predicate));
    }
}
