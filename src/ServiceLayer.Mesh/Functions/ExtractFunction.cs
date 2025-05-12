using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Functions;

public class ExtractFunction
{
    private readonly ILogger _logger;
    private readonly IMeshInboxService _meshInboxService;
    private readonly ServiceLayerDbContext _serviceLayerDbContext;
    private readonly QueueClient _queueClient;
    private readonly BlobContainerClient _blobContainerClient;

    public ExtractFunction(ILogger logger, IMeshInboxService meshInboxService, ServiceLayerDbContext serviceLayerDbContext, QueueClient queueClient, BlobContainerClient blobClient)
    {
        _logger = logger;
        _meshInboxService = meshInboxService;
        _serviceLayerDbContext = serviceLayerDbContext;
        _queueClient = queueClient;
        _blobContainerClient = blobClient;
    }

    [Function("ExtractFunction")]
    public async Task Run([QueueTrigger("my-local-queue")] ExtractQueueMessage message) // TODO: Queue name
    {
        _logger.LogInformation($"ExtractFunction started at: {DateTime.Now}");

        await using var transaction = await _serviceLayerDbContext.Database.BeginTransactionAsync();

        var file = await _serviceLayerDbContext.MeshFiles
            .FirstOrDefaultAsync(f => f.FileId == message.FileId);

        if (file == null)
        {
            // TODO - do we want to throw exception or just exit silently?
            throw new InvalidOperationException("File not found");
        }

        // We only want to extract files if they are in a Discovered state,
        //   or are in an Extracting state and have been last touched over 12 hours ago.
        var expectedStatuses = new[] { MeshFileStatus.Discovered, MeshFileStatus.Extracting };
        if (!expectedStatuses.Contains(file.Status) ||
            file.Status == MeshFileStatus.Extracting && file.LastUpdatedUtc > DateTime.UtcNow.AddHours(-12))
        {
            // TODO - do we want to throw exception or just exit silently?
            throw new InvalidOperationException("File is not in expected status");
        }

        file.Status = MeshFileStatus.Extracting;
        file.LastUpdatedUtc = DateTime.UtcNow;

        await _serviceLayerDbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        var mailboxId = Environment.GetEnvironmentVariable("MeshMailboxId")
            ?? throw new InvalidOperationException($"Environment variable 'MeshMailboxId' is not set or is empty.");

        var meshResponse = await _meshInboxService.GetMessageByIdAsync(mailboxId, file.FileId);
        if (!meshResponse.IsSuccessful)
        {
            // TODO - what to do if unsuccessful?
            throw new InvalidOperationException($"Mesh extraction failed: {meshResponse.Error}");
        }

        await UploadFileToBlobStorage(new BlobFile(meshResponse.Response.FileAttachment.Content, mailboxId));
    }

    public async Task<bool> UploadFileToBlobStorage(BlobFile blobFile, bool overwrite = false)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobFile.FileName);

        await _blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        try
        {
            await blobClient.UploadAsync(blobFile.Data, overwrite: overwrite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There has been a problem while uploading the file: {Message}", ex.Message);
            return false;
        }

        return true;
    }
}

public class BlobFile
{
    public BlobFile(byte[] bytes, string fileName)
    {
        Data = new MemoryStream(bytes);
        FileName = fileName;
    }
    public BlobFile(Stream stream, string fileName)
    {
        Data = stream;
        FileName = fileName;
    }

    public Stream Data { get; set; }
    public string FileName { get; set; }
}

public class ExtractQueueMessage
{
    public string FileId { get; set; }
}
