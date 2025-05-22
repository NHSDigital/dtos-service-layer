using Google.Protobuf.WellKnownTypes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLayer.Data;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Storage;

namespace ServiceLayer.Mesh.Functions;

public class FileTransformFunction(
    ILogger<FileTransformFunction> logger,
    IFileTransformFunctionConfiguration configuration,
    ServiceLayerDbContext serviceLayerDbContext,
    IFileTransformQueueClient fileTransformQueueClient,
    IMeshFilesBlobStore meshFileBlobStore,
    IEnumerable<IFileTransformer> fileTransformers)
{
    [Function("FileTransformFunction")]
    public async Task Run([QueueTrigger("%FileTransformQueueName%")] FileTransformQueueMessage message)
    {
        await using var transaction = await serviceLayerDbContext.Database.BeginTransactionAsync();

        var file = await GetFileAsync(message.FileId);
        if (file == null)
        {
            return;
        }

        if (!IsFileSuitableForTransformation(file))
        {
            return;
        }

        await UpdateFileStatusForTransformation(file);
        await transaction.CommitAsync();

        try
        {
            await ProcessFileTransformation(file, message);
        }
        catch (Exception e)
        {
            await HandleTransformationError(file, message, ex);
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

    private bool IsFileSuitableForTransformation(MeshFile file)
    {
        // We only want to transform files if they are in a Extracted state,
        // or are in a Transforming state and were last touched over 12 hours ago.
        var expectedStatuses = new[] { MeshFileStatus.Extracted, MeshFileStatus.Transforming };
        if (!expectedStatuses.Contains(file.Status) ||
            (file.Status == MeshFileStatus.Transforming && file.LastUpdatedUtc > DateTime.UtcNow.AddHours(-configuration.StaleHours)))
        {
            logger.LogWarning(
                "File with id: {FileId} found in MeshFiles table but is not suitable for transformation. Status: {Status}, LastUpdatedUtc: {LastUpdatedUtc}.",
                file.FileId,
                file.Status,
                file.LastUpdatedUtc.ToTimestamp());
            return false;
        }
        return true;
    }

    private async Task UpdateFileStatusForTransformation(MeshFile file)
    {
        file.Status = MeshFileStatus.Transforming;
        file.LastUpdatedUtc = DateTime.UtcNow;
        await serviceLayerDbContext.SaveChangesAsync();
    }

    private async Task ProcessFileTransformation(MeshFile file)
    {
        var fileContent = await meshFileBlobStore.DownloadAsync(file);

        var transformer = fileTransformers.FirstOrDefault(f => f.HandlesFileType == file.FileType);
        if (transformer == null)
        {
            throw new NotImplementedException($"No transformer registered for file type: {file.FileType}");
        }

        var validationErrors = await transformer.TransformFileAsync(fileContent, file);

        if (validationErrors.Any())
        {
            file.ValidationErrors = SerializeValidationErrors(validationErrors);
        }

        file.Status = MeshFileStatus.Transformed;
        file.LastUpdatedUtc = DateTime.UtcNow;
        await serviceLayerDbContext.SaveChangesAsync();
    }

    private async Task HandleTransformationError(MeshFile file, FileTransformQueueMessage message, Exception ex)
    {
        logger.LogError(ex, "An exception occurred during file transformation for fileId: {fileId}", message.FileId);
        file.Status = MeshFileStatus.FailedTransform;
        file.LastUpdatedUtc = DateTime.UtcNow;
        await serviceLayerDbContext.SaveChangesAsync();
        await fileTransformQueueClient.SendToPoisonQueueAsync(message);
    }

    private string SerializeValidationErrors(IList<ValidationError> validationErrors)
    {
        // TODO
        return "";
    }
}
