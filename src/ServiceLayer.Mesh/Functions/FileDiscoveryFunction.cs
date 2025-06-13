using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Data;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Messaging;

namespace ServiceLayer.Mesh.Functions;

public class FileDiscoveryFunction(
    ILogger<FileDiscoveryFunction> logger,
    IFileDiscoveryFunctionConfiguration configuration,
    IMeshInboxService meshInboxService,
    ServiceLayerDbContext serviceLayerDbContext,
    IFileExtractQueueClient fileExtractQueueClient)
    : MeshFileFunctionBase(serviceLayerDbContext)
{
    [Function("FileDiscoveryFunction")]
    public async Task Run([TimerTrigger("%FileDiscoveryTimerExpression%")] TimerInfo myTimer)
    {
        logger.LogInformation("{FunctionName} started.", nameof(FileDiscoveryFunction));

        var response = await meshInboxService.GetMessagesAsync(configuration.NbssMeshMailboxId);

        // TODO - check if response.IsSuccessful before proceeding to dereference the Response.Messages
        foreach (var messageId in response.Response.Messages)
        {
            await using var transaction = await ServiceLayerDbContext.Database.BeginTransactionAsync();

            var existing = await ServiceLayerDbContext.MeshFiles
                .AnyAsync(f => f.FileId == messageId);

            if (!existing)
            {
                var file = await CreateMeshFile(messageId);

                await transaction.CommitAsync();
                await fileExtractQueueClient.EnqueueFileExtractAsync(file);
            }
            else
            {
                await transaction.RollbackAsync();
            }
        }
    }

    private async Task<MeshFile> CreateMeshFile(string messageId)
    {
        var now = DateTime.UtcNow;

        var file = new MeshFile
        {
            FileId = messageId,
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = configuration.NbssMeshMailboxId,
            Status = MeshFileStatus.Discovered,
            FirstSeenUtc = now,
            LastUpdatedUtc = now
        };

        ServiceLayerDbContext.MeshFiles.Add(file);

        await UpdateMeshFile(file, MeshFileStatus.Discovered);

        return file;
    }

    protected override FileEventSource Source => FileEventSource.DiscoveryFunction;
}
