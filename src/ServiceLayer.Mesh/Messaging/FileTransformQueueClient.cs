using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Messaging;

public class FileTransformQueueClient(ILogger<FileTransformQueueClient> logger, QueueServiceClient queueServiceClient)
    : QueueClientBase(logger, queueServiceClient, "file-transform"), IFileTransformQueueClient
{
    public async Task EnqueueFileTransformAsync(MeshFile file)
    {
        await SendJsonMessageAsync(new FileTransformQueueMessage { FileId = file.FileId });
    }

    public async Task SendToPoisonQueueAsync(FileTransformQueueMessage message)
    {
        await base.SendToPoisonQueueAsync(message);
    }
}
