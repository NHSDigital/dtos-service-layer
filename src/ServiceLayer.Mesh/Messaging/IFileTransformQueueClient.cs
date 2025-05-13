using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Messaging;

internal interface IFileTransformQueueClient
{
    Task EnqueueFileTransformAsync(MeshFile file);
    Task SendToPoisonQueueAsync(FileTransformQueueMessage message);
}
