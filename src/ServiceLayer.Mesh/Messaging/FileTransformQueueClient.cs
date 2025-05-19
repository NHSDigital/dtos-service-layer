using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Configuration;

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
