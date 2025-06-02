using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace ServiceLayer.Mesh.Tests;

[CollectionDefinition("DockerComposeCollection")]
public class DockerComposeCollection : ICollectionFixture<DockerComposeFixture>
{
}

[Collection("DockerComposeCollection")]
public class IntegrationTests
{
    private static async Task WaitForHealthyService()
    {
        bool environmentIsUp = false;

        while (environmentIsUp == false)
        {
            var response = await HttpHelper.SendHttpRequestAsync(HttpMethod.Get, "http://localhost:7072/api/health");
            if (response.IsSuccessStatusCode)
            {
                environmentIsUp = true;
            }
            else
            {
                await Task.Delay(1000);
            }
        }
    }

    [Fact]
    public async Task EndToEndTest()
    {
        // Arrange
        await WaitForHealthyService();

        await SendFileToMeshInbox("KMK_20250212095121_APPT_87.dat");

        await Task.Delay(5000);
    }

    private static async Task SendFileToMeshInbox(string fileName)
    {
        byte[] binaryData = await File.ReadAllBytesAsync($"TestData/{fileName}");
        var content = new ByteArrayContent(binaryData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await HttpHelper.SendHttpRequestAsync(
            HttpMethod.Post,
            "http://localhost:8700/messageexchange/X26ABC1/outbox",
            content,
            headers =>
            {
                headers.Add("Authorization", "NHSMESH X26ABC1:a42f77b9-58de-4b45-b599-2d5bf320b44d:0:202407291437:e3005627136e01706efabcfe72269bc8da3192e90a840ab344ab7f82a39bb5c6");
                headers.Add("Mex-Filename", fileName);
                headers.Add("Mex-From", "X26ABC1");
                headers.Add("Mex-To", "X26ABC1");
                headers.Add("Mex-Workflowid", "API-DOCS-TEST");
            }
        );
    }
}

public static class HttpHelper
{
    private static readonly HttpClient _client = new HttpClient();

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

        // Customize headers if provided
        configureHeaders?.Invoke(request.Headers);

        try
        {
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode(); // Throw if not a success status
            return response;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Request failed: {ex.Message}");
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }
}

public class DockerComposeFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        // Start Docker Compose
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "compose up -d mesh-ingest azurite db db-migrations",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"docker compose up failed, error: {process.StandardError.ReadToEnd()}");
        }
    }

    public async Task DisposeAsync()
    {
        // Stop Docker Compose
        var stopInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "compose down",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(stopInfo);
        await process.WaitForExitAsync();
    }
}
