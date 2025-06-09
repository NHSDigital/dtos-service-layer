using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Data;

namespace ServiceLayer.Mesh.Tests;

[CollectionDefinition("DockerComposeCollection")]
public class DockerComposeCollection : ICollectionFixture<DockerComposeFixture>
{
}

[Collection("DockerComposeCollection")]
public class IntegrationTests
{
    [Fact]
    public async Task FileUploadedToMesh_FileIsUploadedToBlobContainerAndInsertedIntoDb()
    {
        // Arrange
        await WaitForHealthyService();

        // Act
        var fileId = await SendFileToMeshInbox("KMK_20250212095121_APPT_87.dat");

        // Wait to allow functions to ingest the file. The CRON timer trigger for the FileDiscovery function must be considered.
        await Task.Delay(45000);

        // Assert
        Assert.NotNull(fileId);
        Assert.True(await WasFileUploadedToBlobContainer(fileId));
        Assert.True(await WasFileInsertedIntoDatabase(fileId));
    }

    private static async Task WaitForHealthyService()
    {
        bool isServiceHealthy = false;

        while (isServiceHealthy == false)
        {
            var response = await HttpHelper.SendHttpRequestAsync(HttpMethod.Get, "http://localhost:7072/api/health");
            if (response.IsSuccessStatusCode)
            {
                isServiceHealthy = true;
            }
            else
            {
                await Task.Delay(5000);
            }
        }

        Console.WriteLine("Mesh Ingest Service is healthy and ready to start ingesting files");
    }

    private static async Task<string?> SendFileToMeshInbox(string fileName)
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

        string responseBody = await response.Content.ReadAsStringAsync();

        var responseObject = JsonSerializer.Deserialize<MeshResponse>(responseBody);

        return responseObject?.MessageID;
    }

    private static async Task<bool> WasFileUploadedToBlobContainer(string fileId)
    {
        var blobConnectionString = "";

        var containerClient = new BlobContainerClient(blobConnectionString, "incoming-mesh-files");

        try
        {
            var blobClient = containerClient.GetBlobClient($"NbssAppointmentEvents/{fileId}");

            BlobProperties properties = await blobClient.GetPropertiesAsync();
            return true; // If we get properties, the blob exists
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> WasFileInsertedIntoDatabase(string fileId)
    {
        var connectionString = "";
        var options = new DbContextOptionsBuilder<ServiceLayerDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var context = new ServiceLayerDbContext(options);

        return await context.MeshFiles.AnyAsync(x => x.FileId == fileId);
    }

    public class MeshResponse
    {
        [JsonPropertyName("messageID")]
        public required string MessageID { get; set; }
    }
}

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
            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Request failed: {ex.Message}");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
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
            Arguments = "compose up -d mesh-ingest mesh-sandbox azurite db db-migrations",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);

        if (process == null)
        {
            throw new Exception("Failed to start the Docker process.");
        }

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Docker process started but failed, error: {process.StandardError.ReadToEnd()}");
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

        if (process == null)
        {
            throw new Exception("Failed to start the Docker process.");
        }

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Docker process started but failed, error: {process.StandardError.ReadToEnd()}");
        }
    }
}
