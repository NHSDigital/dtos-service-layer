using Azure.Storage.Queues;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Configuration;

namespace ServiceLayer.Mesh.Messaging;

public class FileExtractQueueClient(
    IFileExtractQueueClientConfiguration configuration,
    QueueServiceClient queueServiceClient)
    : QueueClientBase(queueServiceClient), IFileExtractQueueClient
{
    public async Task EnqueueFileExtractAsync(MeshFile file)
    {
        await SendJsonMessageAsync(new FileExtractQueueMessage { FileId = file.FileId });
    }

    public async Task SendToPoisonQueueAsync(FileExtractQueueMessage message)
    {
        await base.SendToPoisonQueueAsync(message);
    }

    protected override string QueueName => configuration.FileExtractQueueName;
}
