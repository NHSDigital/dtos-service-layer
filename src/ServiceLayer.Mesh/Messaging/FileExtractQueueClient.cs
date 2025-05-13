using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Messaging;

public class FileExtractQueueClient(ILogger<FileExtractQueueClient> logger, QueueServiceClient queueServiceClient)
    : QueueClientBase(logger, queueServiceClient, "file-extract"), IFileExtractQueueClient
{
    public async Task EnqueueFileExtractAsync(MeshFile file)
    {
        await SendJsonMessageAsync(new FileExtractQueueMessage { FileId = file.FileId });
    }

    public async Task SendToPoisonQueueAsync(FileExtractQueueMessage message)
    {
        await base.SendToPoisonQueueAsync(message);
    }
}
