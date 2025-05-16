using ServiceLayer.Data.Models;

namespace ServiceLayer.Mesh.Messaging;

public interface IFileTransformQueueClient
{
    Task EnqueueFileTransformAsync(MeshFile file);
    Task SendToPoisonQueueAsync(FileTransformQueueMessage message);
}
