using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Functions
{
    public class DiscoveryFunction(
        ILogger<DiscoveryFunction> logger,
        IMeshInboxService meshInboxService,
        ServiceLayerDbContext serviceLayerDbContext,
        IFileExtractQueueClient fileExtractQueueClient)
    {
        [Function("DiscoveryFunction")]
        public async Task Run([TimerTrigger("%DiscoveryTimerExpression%")] TimerInfo myTimer)
        {
            logger.LogInformation($"DiscoveryFunction started at: {DateTime.Now}");

            var mailboxId = Environment.GetEnvironmentVariable("BSSMailBox")
                ?? throw new InvalidOperationException($"Environment variable 'BSSMailBox' is not set or is empty.");

            var response = await meshInboxService.GetMessagesAsync(mailboxId);

            foreach (var messageId in response.Response.Messages)
            {
                await using var transaction = await serviceLayerDbContext.Database.BeginTransactionAsync();

                var existing = await serviceLayerDbContext.MeshFiles
                    .AnyAsync(f => f.FileId == messageId);

                if (!existing)
                {
                    var file = new MeshFile
                    {
                        FileId = messageId,
                        FileType = MeshFileType.NbssAppointmentEvents,
                        MailboxId = mailboxId,
                        Status = MeshFileStatus.Discovered,
                        FirstSeenUtc = DateTime.UtcNow,
                        LastUpdatedUtc = DateTime.UtcNow
                    };

                    serviceLayerDbContext.MeshFiles.Add(file);

                    await serviceLayerDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await fileExtractQueueClient.EnqueueFileExtractAsync(file);
                }
                else
                {
                    await transaction.RollbackAsync();
                }
            }
        }
    }
}
