using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Messaging;

public class FileTransformQueueClient(
    ILogger<FileTransformQueueClient> logger,
    IFileTransformQueueClientConfiguration configuration,
    QueueServiceClient queueServiceClient)
    : QueueClientBase(logger, queueServiceClient), IFileTransformQueueClient
{
    public async Task EnqueueFileTransformAsync(MeshFile file)
    {
        await SendJsonMessageAsync(new FileTransformQueueMessage { FileId = file.FileId });
    }

    public async Task SendToPoisonQueueAsync(FileTransformQueueMessage message)
    {
        await base.SendToPoisonQueueAsync(message);
    }

    protected override string QueueName => configuration.FileTransformQueueName;
}
