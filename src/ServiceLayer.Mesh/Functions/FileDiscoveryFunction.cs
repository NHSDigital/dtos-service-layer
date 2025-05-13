using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Functions
{
    public class FileDiscoveryFunction(
        ILogger<FileDiscoveryFunction> logger,
        IMeshInboxService meshInboxService,
        ServiceLayerDbContext serviceLayerDbContext,
        IFileExtractQueueClient fileExtractQueueClient)
    {
        [Function("FileDiscoveryFunction")]
        public async Task Run([TimerTrigger("%FileDiscoveryTimerExpression%")] TimerInfo myTimer)
        {
            logger.LogInformation($"DiscoveryFunction started at: {DateTime.Now}");

            // TODO - abstract this out into an injectable configuration interface for testing purposes
            var mailboxId = Environment.GetEnvironmentVariable("NBSSMailBoxId")
                ?? throw new InvalidOperationException($"Environment variable 'NBSSMailBoxId' is not set or is empty.");

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
