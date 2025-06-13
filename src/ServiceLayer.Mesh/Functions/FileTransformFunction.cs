using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLayer.Data;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Storage;

namespace ServiceLayer.Mesh.Functions;

public class FileTransformFunction(
    ILogger<FileTransformFunction> logger,
    IFileTransformFunctionConfiguration configuration,
    ServiceLayerDbContext serviceLayerDbContext,
    IFileTransformQueueClient fileTransformQueueClient,
    IMeshFilesBlobStore meshFileBlobStore,
    IEnumerable<IFileTransformer> fileTransformers) : MeshFileFunctionBase(serviceLayerDbContext)
{
    private static readonly JsonSerializerOptions ValidationErrorJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Function("FileTransformFunction")]
    public async Task Run([QueueTrigger("%FileTransformQueueName%")] FileTransformQueueMessage message)
    {
        logger.LogInformation("{FunctionName} started. Processing fileId: {FileId}", nameof(FileTransformFunction),
            message.FileId);

        await using var transaction = await ServiceLayerDbContext.Database.BeginTransactionAsync();

        var file = await GetFileAsync(message.FileId);
        if (file == null || !IsFileSuitableForTransformation(file))
        {
            return;
        }

        await UpdateMeshFile(file, MeshFileStatus.Transforming);
        await transaction.CommitAsync();

        try
        {
            await ProcessFileTransformation(file);
        }
        catch (Exception ex)
        {
            await HandleTransformationError(file, message, ex);
        }
    }

    private async Task<MeshFile?> GetFileAsync(string fileId)
    {
        var file = await ServiceLayerDbContext.MeshFiles
            .FirstOrDefaultAsync(f => f.FileId == fileId);

        if (file == null)
        {
            logger.LogWarning("File with id: {FileId} not found in MeshFiles table.", fileId);
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

    private async Task ProcessFileTransformation(MeshFile file)
    {
        var transformer = GetTransformerFor(file.FileType);
        var fileContent = await meshFileBlobStore.DownloadAsync(file);

        var validationErrors = await transformer.TransformFileAsync(fileContent, file);

        if (validationErrors.Any())
        {
            file.ValidationErrors = JsonSerializer.Serialize(validationErrors, ValidationErrorJsonOptions);
            throw new InvalidOperationException("Validation errors encountered");
        }

        await UpdateMeshFile(file, MeshFileStatus.Transformed);
    }

    private IFileTransformer GetTransformerFor(MeshFileType type)
    {
        try
        {
            return fileTransformers.SingleOrDefault(t => t.CanHandle(type))
                ?? throw new InvalidOperationException($"No transformer registered to handle file type: {type}");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("more than one"))
        {
            throw new InvalidOperationException(
                $"Multiple transformers found for file type: {type}. This is likely a configuration error.", ex);
        }
    }

    private async Task HandleTransformationError(MeshFile file, FileTransformQueueMessage message, Exception ex)
    {
        logger.LogError(ex, "An exception occurred during file transformation for fileId: {FileId}", message.FileId);
        await UpdateMeshFile(file, MeshFileStatus.FailedTransform);
        await fileTransformQueueClient.SendToPoisonQueueAsync(message);
    }

    protected override FileEventSource Source => FileEventSource.TransformFunction;
}
