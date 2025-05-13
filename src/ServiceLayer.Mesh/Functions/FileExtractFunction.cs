using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Models;
using ServiceLayer.Mesh.Storage;

namespace ServiceLayer.Mesh.Functions;

public class FileExtractFunction(
    ILogger logger,
    IMeshInboxService meshInboxService,
    ServiceLayerDbContext serviceLayerDbContext,
    IFileTransformQueueClient fileTransformQueueClient,
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

        // TODO - approx after this point we'll need to wrap everything in a try-catch. On failure we should
        //        update the meshfile to FailedExtract, and move the message to the poison queue

        var mailboxId = Environment.GetEnvironmentVariable("MeshMailboxId")
            ?? throw new InvalidOperationException($"Environment variable 'MeshMailboxId' is not set or is empty.");

        var meshResponse = await meshInboxService.GetMessageByIdAsync(mailboxId, file.FileId);
        if (!meshResponse.IsSuccessful)
        {
            // TODO - what to do if unsuccessful?
            throw new InvalidOperationException($"Mesh extraction failed: {meshResponse.Error}");
        }

        await meshFileBlobStore.UploadAsync(file, meshResponse.Response.FileAttachment.Content);
    }

    public async Task<bool> UploadFileToBlobStorage(BlobFile blobFile, bool overwrite = false)
    {
        var blobClient = blobContainerClient.GetBlobClient(blobFile.FileName);

        await blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        try
        {
            await blobClient.UploadAsync(blobFile.Data, overwrite: overwrite);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There has been a problem while uploading the file: {Message}", ex.Message);
            return false;
        }

        return true;
    }
}

public class BlobFile
{
    public BlobFile(byte[] bytes, string fileName)
    {
        Data = new MemoryStream(bytes);
        FileName = fileName;
    }
    public BlobFile(Stream stream, string fileName)
    {
        Data = stream;
        FileName = fileName;
    }

    public Stream Data { get; set; }
    public string FileName { get; set; }
}


