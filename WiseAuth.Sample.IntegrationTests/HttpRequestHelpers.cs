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
}
