using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Models;
using ServiceLayer.Mesh.Storage;

namespace ServiceLayer.Mesh.Functions;

public class FileTransformFunction(
    ILogger<FileTransformFunction> logger,
    ServiceLayerDbContext serviceLayerDbContext,
    IMeshFilesBlobStore meshFileBlobStore)
{
    [Function("FileTransformFunction")]
    public async Task Run([QueueTrigger("%FileTransformQueueName%")] FileTransformQueueMessage message)
    {
        await using var transaction = await serviceLayerDbContext.Database.BeginTransactionAsync();

        var file = await serviceLayerDbContext.MeshFiles
            .FirstOrDefaultAsync(f => f.FileId == message.FileId);

        if (file == null)
        {
            logger.LogWarning("File with id: {fileId} not found in MeshFiles table.", message.FileId);
            return;
        }

        if (file.Status != MeshFileStatus.Extracted)
        {
            logger.LogWarning("File with id: {fileId} found in MeshFiles table but is not suitable for transformation. Status: {status}", message.FileId, file.Status);
            return;
        }

        file.Status = MeshFileStatus.Transforming;
        file.LastUpdatedUtc = DateTime.UtcNow;
        await serviceLayerDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        var fileContent = await meshFileBlobStore.DownloadAsync(file);

        // TODO - take dependency on IEnumerable<IFileTransformer>.
        // After initial common checks against database, find the appropriate implementation of IFileTransformer to handle the functionality that differs between file type.
    }
}
