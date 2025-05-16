using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Models;
using ServiceLayer.Mesh.Configuration;

namespace ServiceLayer.Mesh.Functions;

public class FileRetryFunction(
    ILogger<FileRetryFunction> logger,
    ServiceLayerDbContext serviceLayerDbContext,
    IFileExtractQueueClient fileExtractQueueClient,
    IFileTransformQueueClient fileTransformQueueClient,
    IFileRetryFunctionConfiguration configuration)
{
    [Function("FileRetryFunction")]
    public async Task Run([TimerTrigger("%FileRetryTimerExpression%")] TimerInfo myTimer)
    {
        logger.LogInformation("FileRetryFunction started");

        var staleDateTimeUtc = DateTime.UtcNow.AddHours(-configuration.StaleHours);

        await Task.WhenAll(
            RetryStaleExtractions(staleDateTimeUtc),
            RetryStaleTransformations(staleDateTimeUtc));
    }

    private async Task RetryStaleExtractions(DateTime staleDateTimeUtc)
    {
        var staleFiles = await serviceLayerDbContext.MeshFiles
            .Where(f =>
                (f.Status == MeshFileStatus.Discovered || f.Status == MeshFileStatus.Extracting)
                && f.LastUpdatedUtc <= staleDateTimeUtc)
            .ToListAsync();

        logger.LogInformation($"FileRetryFunction: {staleFiles.Count} stale files found for extraction retry");

        foreach (var file in staleFiles)
        {
            await fileExtractQueueClient.EnqueueFileExtractAsync(file);
            file.LastUpdatedUtc = DateTime.UtcNow;
            await serviceLayerDbContext.SaveChangesAsync();
            logger.LogInformation($"FileRetryFunction: File {file.FileId} enqueued to Extract queue");
        }
    }

    private async Task RetryStaleTransformations(DateTime staleDateTimeUtc)
    {
        var staleFiles = await serviceLayerDbContext.MeshFiles
            .Where(f =>
                (f.Status == MeshFileStatus.Extracted || f.Status == MeshFileStatus.Transforming)
                && f.LastUpdatedUtc <= staleDateTimeUtc)
            .ToListAsync();

        logger.LogInformation($"FileRetryFunction: {staleFiles.Count} stale files found for transforming retry");

        foreach (var file in staleFiles)
        {
            await fileTransformQueueClient.EnqueueFileTransformAsync(file);
            file.LastUpdatedUtc = DateTime.UtcNow;
            await serviceLayerDbContext.SaveChangesAsync();
            logger.LogInformation($"FileRetryFunction: File {file.FileId} enqueued to Transform queue");
        }
    }
}
