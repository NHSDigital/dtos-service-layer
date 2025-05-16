using Google.Protobuf.WellKnownTypes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Models;
using ServiceLayer.Mesh.Storage;

namespace ServiceLayer.Mesh.Functions;

public class FileTransformFunction(
    ILogger<FileTransformFunction> logger,
    ServiceLayerDbContext serviceLayerDbContext,
    IMeshFilesBlobStore meshFileBlobStore,
    IFileTransformFunctionConfiguration configuration)
{
    [Function("FileTransformFunction")]
    public async Task Run([QueueTrigger("%FileTransformQueueName%")] FileTransformQueueMessage message)
    {
        await using var transaction = await serviceLayerDbContext.Database.BeginTransactionAsync();

        var file = await serviceLayerDbContext.MeshFiles.FirstOrDefaultAsync(f => f.FileId == message.FileId);

        if (file == null)
        {
            logger.LogWarning("File with id: {fileId} not found in MeshFiles table.", message.FileId);
            return;
        }

        if (!IsFileSuitableForTransformation(file))
        {
            return;
        }

        await UpdateFileStatusForTransformation(file);
        await transaction.CommitAsync();

        var fileContent = await meshFileBlobStore.DownloadAsync(file);

        // TODO - take dependency on IEnumerable<IFileTransformer>.
        // After initial common checks against database, find the appropriate implementation of IFileTransformer to handle the functionality that differs between file type.
    }

    private async Task UpdateFileStatusForTransformation(MeshFile file)
    {
        file.Status = MeshFileStatus.Transforming;
        file.LastUpdatedUtc = DateTime.UtcNow;
        await serviceLayerDbContext.SaveChangesAsync();
    }

    private bool IsFileSuitableForTransformation(MeshFile file)
    {
        // We only want to transform files if they are in a Extracted state,
        // or are in a Transforming state and were last touched over 12 hours ago.
        var expectedStatuses = new[] { MeshFileStatus.Extracted, MeshFileStatus.Transforming };
        if (!expectedStatuses.Contains(file.Status) ||
            (file.Status == MeshFileStatus.Transforming && file.LastUpdatedUtc > DateTime.UtcNow.AddHours(-configuration.StaleHours)))
        {
            logger.LogWarning(
                "File with id: {fileId} found in MeshFiles table but is not suitable for transformation. Status: {status}, LastUpdatedUtc: {lastUpdatedUtc}.",
                file.FileId,
                file.Status,
                file.LastUpdatedUtc.ToTimestamp());
            return false;
        }
        return true;
    }
}
