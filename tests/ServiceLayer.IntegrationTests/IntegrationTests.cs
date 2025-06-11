using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Data;
using ServiceLayer.TestUtilities;

namespace ServiceLayer.IntegrationTests;

[CollectionDefinition("DockerComposeCollection")]
public class DockerComposeCollection : ICollectionFixture<DockerComposeFixture>
{
}

[Collection("DockerComposeCollection")]
public class IntegrationTests
{
    private readonly string _azuriteAccountKey = Environment.GetEnvironmentVariable("AZURITE_ACCOUNT_KEY")
        ?? throw new InvalidOperationException($"Environment variable 'AZURITE_ACCOUNT_KEY' is not set.");
    private readonly string _azuriteAccountName = Environment.GetEnvironmentVariable("AZURITE_ACCOUNT_NAME")
        ?? throw new InvalidOperationException($"Environment variable 'AZURITE_ACCOUNT_NAME' is not set.");
    private readonly string _azuriteBlobPort = Environment.GetEnvironmentVariable("AZURITE_BLOB_PORT")
        ?? throw new InvalidOperationException($"Environment variable 'AZURITE_BLOB_PORT' is not set.");
    private readonly string _meshIngestPort = Environment.GetEnvironmentVariable("MESH_INGEST_PORT")
        ?? throw new InvalidOperationException($"Environment variable 'MESH_INGEST_PORT' is not set.");
    private readonly string _meshSandboxPort = Environment.GetEnvironmentVariable("MESH_SANDBOX_PORT")
        ?? throw new InvalidOperationException($"Environment variable 'MESH_SANDBOX_PORT' is not set.");
    private readonly string _meshBlobContainerName = Environment.GetEnvironmentVariable("MESH_BLOB_CONTAINER_NAME")
        ?? throw new InvalidOperationException($"Environment variable 'MESH_BLOB_CONTAINER_NAME' is not set.");
    private readonly string _databaseConnectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
        ?? throw new InvalidOperationException($"Environment variable 'DATABASE_CONNECTION_STRING' is not set.");

    [Fact]
    public async Task FileSentToMeshInbox_FileIsUploadedToBlobContainerAndInsertedIntoDb()
    {
        // Arrange
        await WaitForHealthyService();

        // Act
        var fileId = await SendFileToMeshInbox("KMK_20250212095121_APPT_87.dat");

        // Wait to allow functions to ingest the file
        await Task.Delay(45000);

        // Assert
        Assert.NotNull(fileId);
        Assert.True(await WasFileUploadedToBlobContainer(fileId));
        Assert.True(await WasFileInsertedIntoDatabase(fileId));
    }

    private async Task WaitForHealthyService()
    {
        Console.WriteLine("Waiting for Mesh Ingest Service health check to pass...");

        int attemptCounter = 0;

        while (attemptCounter < 10)
        {
            var response = await HttpHelper.SendHttpRequestAsync(HttpMethod.Get, $"http://localhost:{_meshIngestPort}/api/health");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Mesh Ingest Service is healthy and ready to start ingesting files.");
                return;
            }

            Console.WriteLine("Mesh Ingest Service is unhealthy");
            attemptCounter++;
            await Task.Delay(5000);
        }

        Console.WriteLine("Max attempts reached. Mesh Ingest Service is still unhealthy.");
        throw new TimeoutException("Timed out waiting on Mesh Ingest Service health check");
    }

    private async Task<string?> SendFileToMeshInbox(string fileName)
    {
        byte[] binaryData = await File.ReadAllBytesAsync($"TestData/{fileName}");
        var content = new ByteArrayContent(binaryData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await HttpHelper.SendHttpRequestAsync(
            HttpMethod.Post,
            $"http://localhost:{_meshSandboxPort}/messageexchange/X26ABC1/outbox",
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

    private async Task<bool> WasFileUploadedToBlobContainer(string fileId)
    {
        var blobConnectionString = $"DefaultEndpointsProtocol=http;AccountName={_azuriteAccountName};AccountKey={_azuriteAccountKey};BlobEndpoint=http://localhost:{_azuriteBlobPort}/{_azuriteAccountName}";

        var containerClient = new BlobContainerClient(blobConnectionString, _meshBlobContainerName);

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

    private async Task<bool> WasFileInsertedIntoDatabase(string fileId)
    {
        var options = new DbContextOptionsBuilder<ServiceLayerDbContext>()
            .UseSqlServer(_databaseConnectionString)
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

public class DockerComposeFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        Console.WriteLine("Starting up docker containers...");

        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "compose --env-file .env.tests up -d svclyr-mesh-ingest mesh-sandbox azurite db db-migrations",
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

        Console.WriteLine("Docker containers successfully started");
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("Stopping docker containers...");

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

        Console.WriteLine("Docker containers stopped");
    }
}
