using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Models;
using ServiceLayer.Mesh.Configuration;

namespace ServiceLayer.Mesh.Functions
{
    public class FileRetryFunction(
        ILogger<FileRetryFunction> logger,
        IMeshInboxService meshInboxService,
        ServiceLayerDbContext serviceLayerDbContext,
        IFileExtractQueueClient fileExtractQueueClient,
        IFileTransformQueueClient fileTransformQueueClient,
        IFileRetryFunctionConfiguration configuration)
    {
        [Function("FileRetryFunction")]
        public async Task Run([TimerTrigger("%FileRetryTimerExpression%")] TimerInfo myTimer)
        {
            logger.LogInformation($"FileRetryFunction started at: {DateTime.Now}");

            var overdueAnUpdateDateTime = DateTime.UtcNow.AddHours(-Convert.ToInt32(configuration.StaleHours));

            var files = await serviceLayerDbContext.MeshFiles
                .Where(f =>
                    (f.Status == MeshFileStatus.Discovered ||
                     f.Status == MeshFileStatus.Extracting ||
                     f.Status == MeshFileStatus.Extracted ||
                     f.Status == MeshFileStatus.Transforming) && f.LastUpdatedUtc <= overdueAnUpdateDateTime)
                .ToListAsync();

            logger.LogInformation($"FileRetryFunction: {files.Count} stale files found");

            foreach (var file in files)
            {
                if (file.Status == MeshFileStatus.Discovered || file.Status == MeshFileStatus.Extracting)
                {
                    await fileExtractQueueClient.EnqueueFileExtractAsync(file);
                    file.LastUpdatedUtc = DateTime.UtcNow;
                    await serviceLayerDbContext.SaveChangesAsync();
                    logger.LogInformation($"FileRetryFunction: File {file.FileId} enqueued to Extract queue");
                }
                else if (file.Status == MeshFileStatus.Extracted || file.Status == MeshFileStatus.Transforming)
                {
                    await fileTransformQueueClient.EnqueueFileTransformAsync(file);
                    file.LastUpdatedUtc = DateTime.UtcNow;
                    await serviceLayerDbContext.SaveChangesAsync();
                    logger.LogInformation($"FileRetryFunction: File {file.FileId} enqueued to Transform queue");
                }
            }
        }
    }
}
