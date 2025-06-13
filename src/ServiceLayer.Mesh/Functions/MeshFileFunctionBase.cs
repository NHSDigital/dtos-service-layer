using ServiceLayer.Data;
using ServiceLayer.Data.Models;

namespace ServiceLayer.Mesh.Functions;

public abstract class MeshFileFunctionBase(ServiceLayerDbContext serviceLayerDbContext)
{
    protected ServiceLayerDbContext ServiceLayerDbContext { get; } = serviceLayerDbContext;

    protected abstract FileEventSource Source { get; }

    protected async Task UpdateMeshFile(MeshFile file, MeshFileStatus status)
    {
        var now = DateTime.UtcNow;

        file.Status = status;
        file.LastUpdatedUtc = now;

        var fileEvent = new MeshFileEvent
        {
            FileId = file.FileId,
            Status = status,
            TimestampUtc = now,
            Source = Source
        };

        ServiceLayerDbContext.MeshFileEvents.Add(fileEvent);

        await ServiceLayerDbContext.SaveChangesAsync();
    }
}
