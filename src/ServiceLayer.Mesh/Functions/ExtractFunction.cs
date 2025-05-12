using Azure.Storage.Blobs;
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
    private readonly BlobClient _blobClient;


    public ExtractFunction(ILogger logger, IMeshInboxService meshInboxService, ServiceLayerDbContext serviceLayerDbContext, QueueClient queueClient, BlobClient blobClient)
    {
        _logger = logger;
        _meshInboxService = meshInboxService;
        _serviceLayerDbContext = serviceLayerDbContext;
        _queueClient = queueClient;
        _blobClient = blobClient;
    }

    [Function("ExtractFunction")]
    public async Task Run([QueueTrigger("TODO")] ExtractQueueMessage message)
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

        var mailboxId = Environment.GetEnvironmentVariable("BSSMailBox")
            ?? throw new InvalidOperationException($"Environment variable 'BSSMailBox' is not set or is empty.");

        var meshResponse = await _meshInboxService.GetMessageByIdAsync(mailboxId, file.FileId);
        if (!meshResponse.IsSuccessful)
        {
            // TODO - what to do if unsuccessful?
            throw new InvalidOperationException($"Mesh extraction failed: {meshResponse.Error}");
        }
    }

}

public class ExtractQueueMessage
{
    public string FileId { get; set; }
}
