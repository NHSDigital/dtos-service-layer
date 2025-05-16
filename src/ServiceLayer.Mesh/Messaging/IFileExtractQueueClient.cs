using ServiceLayer.Data.Models;

namespace ServiceLayer.Mesh.Messaging;

public interface IFileExtractQueueClient
{
    Task EnqueueFileExtractAsync(MeshFile file);
    Task SendToPoisonQueueAsync(FileExtractQueueMessage message);
}
