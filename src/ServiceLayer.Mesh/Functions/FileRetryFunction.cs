using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Functions
{
    public class FileRetryFunction(
        ILogger<FileRetryFunction> logger,
        IMeshInboxService meshInboxService,
        ServiceLayerDbContext serviceLayerDbContext,
        IFileExtractQueueClient fileExtractQueueClient)
    {
        [Function("FileRetryFunction")]
        public async Task Run([TimerTrigger("%FileRetryTimerExpression%")] TimerInfo myTimer)
        {
            var twelveHoursAgo = DateTime.UtcNow.AddHours(-12);

            var files = await serviceLayerDbContext.MeshFiles
                .Where(f =>
                    (f.Status == MeshFileStatus.Discovered ||
                     f.Status == MeshFileStatus.Extracting ||
                     f.Status == MeshFileStatus.Extracted ||
                     f.Status == MeshFileStatus.Transforming) && f.LastUpdatedUtc <= twelveHoursAgo)
                .ToListAsync();

            foreach (var file in files)
            {
                if (file.Status == MeshFileStatus.Discovered || file.Status == MeshFileStatus.Extracting)
                {
                    await fileExtractQueueClient.EnqueueFileExtractAsync(file);
                    file.LastUpdatedUtc = DateTime.UtcNow;
                    await serviceLayerDbContext.SaveChangesAsync();
                }
                else if (file.Status == MeshFileStatus.Extracted || file.Status == MeshFileStatus.Transforming)
                {
                    //await fileExtractQueueClient.EnqueueFileExtractAsync(file);
                    file.LastUpdatedUtc = DateTime.UtcNow;
                    await serviceLayerDbContext.SaveChangesAsync();
                }
            }
        }
    }
}
