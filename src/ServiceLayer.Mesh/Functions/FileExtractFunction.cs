using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Models;
using ServiceLayer.Mesh.Storage;

namespace ServiceLayer.Mesh.Functions;

public class FileExtractFunction(
    ILogger<FileExtractFunction> logger,
    IFileExtractFunctionConfiguration configuration,
    IMeshInboxService meshInboxService,
    ServiceLayerDbContext serviceLayerDbContext,
    IFileTransformQueueClient fileTransformQueueClient,
    IFileExtractQueueClient fileExtractQueueClient,
    IMeshFilesBlobStore meshFileBlobStore)
{
    [Function("FileExtractFunction")]
    public async Task Run([QueueTrigger("%FileExtractQueueName%")] FileExtractQueueMessage message)
    {
        logger.LogInformation("{functionName} started at: {time}", nameof(FileDiscoveryFunction), DateTime.UtcNow);

        await using var transaction = await serviceLayerDbContext.Database.BeginTransactionAsync();

        var file = await serviceLayerDbContext.MeshFiles
            .FirstOrDefaultAsync(f => f.FileId == message.FileId);

        if (file == null)
        {
            logger.LogWarning("File with id: {fileId} not found in MeshFiles table. Exiting function.", message.FileId);
            return;
        }

        // We only want to extract files if they are in a Discovered state,
        // or are in an Extracting state and were last touched over 12 hours ago.
        var expectedStatuses = new[] { MeshFileStatus.Discovered, MeshFileStatus.Extracting };
        if (!expectedStatuses.Contains(file.Status) ||
            (file.Status == MeshFileStatus.Extracting && file.LastUpdatedUtc > DateTime.UtcNow.AddHours(-12)))
        {
            logger.LogWarning(
                "File with id: {fileId} found in MeshFiles table but has unexpected Status: {status}, LastUpdatedUtc: {lastUpdatedUtc}. Exiting function.",
                message.FileId,
                file.Status,
                file.LastUpdatedUtc);
            return;
        }

        file.Status = MeshFileStatus.Extracting;
        file.LastUpdatedUtc = DateTime.UtcNow;

        await serviceLayerDbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        try
        {
            var meshResponse = await meshInboxService.GetMessageByIdAsync(configuration.NbssMeshMailboxId, file.FileId);
            if (!meshResponse.IsSuccessful)
            {
                throw new InvalidOperationException($"Mesh extraction failed: {meshResponse.Error}");
            }

            var blobPath = await meshFileBlobStore.UploadAsync(file, meshResponse.Response.FileAttachment.Content);

            var meshAcknowledgementResponse = await meshInboxService.AcknowledgeMessageByIdAsync(configuration.NbssMeshMailboxId, message.FileId);
            if (!meshAcknowledgementResponse.IsSuccessful)
            {
                throw new InvalidOperationException($"Mesh acknowledgement failed: {meshAcknowledgementResponse.Error}");
            }

            file.BlobPath = blobPath;
            file.Status = MeshFileStatus.Extracted;
            file.LastUpdatedUtc = DateTime.UtcNow;
            await serviceLayerDbContext.SaveChangesAsync();

            await fileTransformQueueClient.EnqueueFileTransformAsync(file);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception occurred during file extraction for fileId: {fileId}", message.FileId);
            file.Status = MeshFileStatus.FailedExtract;
            file.LastUpdatedUtc = DateTime.UtcNow;
            await serviceLayerDbContext.SaveChangesAsync();
            await fileExtractQueueClient.SendToPoisonQueueAsync(message);
        }
    }
}
