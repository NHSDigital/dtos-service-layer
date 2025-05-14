using Google.Protobuf.WellKnownTypes;
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

        var file = await GetFileAsync(message.FileId);
        if (file == null)
        {
            logger.LogWarning("Exiting function.");
            return;
        }

        if (!IsFileSuitableForExtraction(file))
        {
            logger.LogWarning("Exiting function.");
            return;
        }

        await UpdateFileStatusForExtraction(file);
        await transaction.CommitAsync();

        try
        {
            await ProcessFileExtraction(file, message);
        }
        catch (Exception ex)
        {
            await HandleExtractionError(file, message, ex);
        }
    }

    private async Task<MeshFile?> GetFileAsync(string fileId)
    {
        var file = await serviceLayerDbContext.MeshFiles
            .FirstOrDefaultAsync(f => f.FileId == fileId);

        if (file == null)
        {
            logger.LogWarning("File with id: {fileId} not found in MeshFiles table.", fileId);
        }

        return file;
    }

    private bool IsFileSuitableForExtraction(MeshFile file)
    {
        // We only want to extract files if they are in a Discovered state,
        // or are in an Extracting state and were last touched over 12 hours ago.
        var expectedStatuses = new[] { MeshFileStatus.Discovered, MeshFileStatus.Extracting };
        if (!expectedStatuses.Contains(file.Status) ||
            (file.Status == MeshFileStatus.Extracting && file.LastUpdatedUtc > DateTime.UtcNow.AddHours(-12)))
        {
            logger.LogWarning(
                "File with id: {fileId} found in MeshFiles table but is not suitable for extraction. Status: {status}, LastUpdatedUtc: {lastUpdatedUtc}.",
                file.FileId,
                file.Status,
                file.LastUpdatedUtc.ToTimestamp());
            return false;
        }
        return true;
    }

    private async Task UpdateFileStatusForExtraction(MeshFile file)
    {
        file.Status = MeshFileStatus.Extracting;
        file.LastUpdatedUtc = DateTime.UtcNow;
        await serviceLayerDbContext.SaveChangesAsync();
    }

    private async Task ProcessFileExtraction(MeshFile file, FileExtractQueueMessage message)
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
            logger.LogWarning("Mesh acknowledgement failed: {error}.\nThis is not a fatal error so processing will continue.", meshAcknowledgementResponse.Error);
        }

        file.BlobPath = blobPath;
        file.Status = MeshFileStatus.Extracted;
        file.LastUpdatedUtc = DateTime.UtcNow;
        await serviceLayerDbContext.SaveChangesAsync();

        await fileTransformQueueClient.EnqueueFileTransformAsync(file);
    }

    private async Task HandleExtractionError(MeshFile file, FileExtractQueueMessage message, Exception ex)
    {
        logger.LogError(ex, "An exception occurred during file extraction for fileId: {fileId}", message.FileId);
        file.Status = MeshFileStatus.FailedExtract;
        file.LastUpdatedUtc = DateTime.UtcNow;
        await serviceLayerDbContext.SaveChangesAsync();
        await fileExtractQueueClient.SendToPoisonQueueAsync(message);
    }
}
