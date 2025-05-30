using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Moq;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ServiceLayer.Data;

namespace ServiceLayer.Mesh.Tests.Integration;

public class IntegrationTests
{
    private const string ConnectionString = "Server=localhost,1433;User Id=sa;Password=YourPassword123;TrustServerCertificate=True;";

    public IntegrationTests()
    {

    }

    private async Task SetupEnvironment()
    {
        var environment = "development";
        if (environment == null)
        {
            throw new InvalidOperationException("ASPNETCORE_ENVIRONMENT environment variable is not set of is empty.");
        }
        if (environment == "development")
        {
            RunCommand("podman compose up -d");
        }
        if (environment == "production")
        {
            RunCommand("docker compose up -d");
        }

        bool environmentIsUp = false;

        while (environmentIsUp == false)
        {
            var responce = await HttpHelper.SendHttpRequestAsync(HttpMethod.Get, "http://localhost:7072/api/health");
            if (responce.IsSuccessStatusCode)
            {
                environmentIsUp = true;
            }
            else
            {
                await Task.Delay(1000);
            }
        }
    }

    public void Teardown()
    {
        // Stop containers
        RunCommand("docker compose down");
    }

    [Fact]
    public async Task EndToEndTest()
    {
        await SetupEnvironment();

        byte[] binaryData = await File.ReadAllBytesAsync("TestData/KMK_20250212095121_APPT_87.dat");
        var content = new ByteArrayContent(binaryData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await HttpHelper.SendHttpRequestAsync(
            HttpMethod.Post,
            "http://localhost:8700/messageexchange/X26ABC1/outbox",
            content,
            headers =>
            {
                headers.Add("Authorization", "NHSMESH X26ABC1:a42f77b9-58de-4b45-b599-2d5bf320b44d:0:202407291437:e3005627136e01706efabcfe72269bc8da3192e90a840ab344ab7f82a39bb5c6");
                headers.Add("Mex-Filename", "KMK_20250212095121_APPT_87.dat");
                headers.Add("Mex-From", "X26ABC1");
                headers.Add("Mex-To", "X26ABC1");
                headers.Add("Mex-Workflowid", "API-DOCS-TEST");
                headers.Add("User-Agent", "HTTPie");
            }
        );

        await Task.Delay(5000);

        Teardown();
    }

    private void RunCommand(string command)
    {
        var psi = new ProcessStartInfo("cmd", $"/c {command}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Command failed: {command}\n{process.StandardError.ReadToEnd()}");
        }
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
            throw;
        }
    }
}
