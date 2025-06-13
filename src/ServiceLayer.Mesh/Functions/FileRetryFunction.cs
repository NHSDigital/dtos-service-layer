using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLayer.Data;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Configuration;

namespace ServiceLayer.Mesh.Functions;

public class FileRetryFunction(
    ILogger<FileRetryFunction> logger,
    ServiceLayerDbContext serviceLayerDbContext,
    IFileExtractQueueClient fileExtractQueueClient,
    IFileTransformQueueClient fileTransformQueueClient,
    IFileRetryFunctionConfiguration configuration) : MeshFileFunctionBase(serviceLayerDbContext)
{
    [Function("FileRetryFunction")]
    public async Task Run([TimerTrigger("%FileRetryTimerExpression%")] TimerInfo myTimer)
    {
        logger.LogInformation("{functionName} started.", nameof(FileRetryFunction));

        var staleDateTimeUtc = DateTime.UtcNow.AddHours(-configuration.StaleHours);

        await RetryStaleExtractions(staleDateTimeUtc);
        await RetryStaleTransformations(staleDateTimeUtc);
    }

    private async Task RetryStaleExtractions(DateTime staleDateTimeUtc)
    {
        var staleFiles = await ServiceLayerDbContext.MeshFiles
            .Where(f =>
                (f.Status == MeshFileStatus.Discovered || f.Status == MeshFileStatus.Extracting)
                && f.LastUpdatedUtc <= staleDateTimeUtc)
            .ToListAsync();

        logger.LogInformation("FileRetryFunction: {StaleFilesCount} stale files found for extraction retry", staleFiles.Count);

        foreach (var file in staleFiles)
        {
            await fileExtractQueueClient.EnqueueFileExtractAsync(file);
            await UpdateMeshFile(file, file.Status);
            await ServiceLayerDbContext.SaveChangesAsync();
            logger.LogInformation("FileRetryFunction: File {FileFileId} enqueued to Extract queue", file.FileId);
        }
    }

    private async Task RetryStaleTransformations(DateTime staleDateTimeUtc)
    {
        var staleFiles = await ServiceLayerDbContext.MeshFiles
            .Where(f =>
                (f.Status == MeshFileStatus.Extracted || f.Status == MeshFileStatus.Transforming)
                && f.LastUpdatedUtc <= staleDateTimeUtc)
            .ToListAsync();

        logger.LogInformation("FileRetryFunction: {StaleFilesCount} stale files found for transforming retry", staleFiles.Count);

        foreach (var file in staleFiles)
        {
            await fileTransformQueueClient.EnqueueFileTransformAsync(file);
            await UpdateMeshFile(file, file.Status);
            await ServiceLayerDbContext.SaveChangesAsync();
            logger.LogInformation("FileRetryFunction: File {FileFileId} enqueued to Transform queue", file.FileId);
        }
    }

    protected override FileEventSource Source => FileEventSource.RetryFunction;
}
