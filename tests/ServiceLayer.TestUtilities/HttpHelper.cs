using System.Net;
using System.Net.Http.Headers;

namespace ServiceLayer.TestUtilities;

public static class HttpHelper
{
    private static readonly HttpClient _client = new();

    public static async Task<HttpResponseMessage> SendHttpRequestAsync(
        HttpMethod method,
        string url,
        HttpContent? content = null,
        Action<HttpRequestHeaders>? configureHeaders = null)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = content
        };

        configureHeaders?.Invoke(request.Headers);

        try
        {
            var response = await _client.SendAsync(request);
            return response;
        }
        catch
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
    }
}
