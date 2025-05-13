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
    public async Task Run([QueueTrigger("file-extract")] FileExtractQueueMessage message) // TODO: Queue name
    {
        logger.LogInformation($"ExtractFunction started at: {DateTime.Now}");

        await using var transaction = await serviceLayerDbContext.Database.BeginTransactionAsync();

        var file = await serviceLayerDbContext.MeshFiles
            .FirstOrDefaultAsync(f => f.FileId == message.FileId);

        if (file == null)
        {
            // TODO - do we want to throw exception or just exit silently?
            // ANswer - exit silenty
            throw new InvalidOperationException("File not found");
        }

        // We only want to extract files if they are in a Discovered state,
        //   or are in an Extracting state and have been last touched over 12 hours ago.
        var expectedStatuses = new[] { MeshFileStatus.Discovered, MeshFileStatus.Extracting };
        if (!expectedStatuses.Contains(file.Status) ||
            file.Status == MeshFileStatus.Extracting && file.LastUpdatedUtc > DateTime.UtcNow.AddHours(-12))
        {
            // TODO - do we want to throw exception or just exit silently?
            // ANswer - exit silenty
            throw new InvalidOperationException("File is not in expected status");
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
                // TODO - what to do if unsuccessful?
                throw new InvalidOperationException($"Mesh extraction failed: {meshResponse.Error}");
            }

            var blobPath = await meshFileBlobStore.UploadAsync(file, meshResponse.Response.FileAttachment.Content);

            var meshAcknowledgementResponse = await meshInboxService.AcknowledgeMessageByIdAsync(configuration.NbssMeshMailboxId, message.FileId);
            if (!meshAcknowledgementResponse.IsSuccessful)
            {
                // TODO - what to do if unsuccessful?
                throw new InvalidOperationException($"Mesh acknowledgement failed: {meshResponse.Error}");
            }

            file.Status = MeshFileStatus.Extracted;
            file.LastUpdatedUtc = DateTime.UtcNow;
            file.BlobPath = blobPath;
            await serviceLayerDbContext.SaveChangesAsync();

            await fileTransformQueueClient.EnqueueFileTransformAsync(file);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "");
            file.Status = MeshFileStatus.FailedExtract;
            file.LastUpdatedUtc = DateTime.UtcNow;
            await serviceLayerDbContext.SaveChangesAsync();
            await fileExtractQueueClient.SendToPoisonQueueAsync(message);
        }
    }
}
