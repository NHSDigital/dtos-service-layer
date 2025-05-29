using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Functions;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Storage;
using ServiceLayer.TestUtilities;

namespace ServiceLayer.Mesh.Tests.Functions;

public class FileTransformFunctionTests : FunctionTestBase<FileTransformFunction>
{
    private readonly Mock<IMeshFilesBlobStore> _blobStoreMock = new();
    private readonly Mock<IFileTransformQueueClient> _fileTransformQueueClientMock = new();
    private readonly FileTransformFunction _function;
    private readonly List<IFileTransformer> _fileTransformers = new();
    private readonly Mock<IFileTransformer> _fileTransformerMock = new();

    public FileTransformFunctionTests()
    {
        var functionConfigurationMock = new Mock<IFileTransformFunctionConfiguration>();
        functionConfigurationMock.Setup(c => c.StaleHours).Returns(12);

        _fileTransformerMock.Setup(c => c.CanHandle(MeshFileType.NbssAppointmentEvents)).Returns(true);
        _fileTransformers.Add(_fileTransformerMock.Object);

        _function = new FileTransformFunction(
            LoggerMock.Object,
            functionConfigurationMock.Object,
            DbContext,
            _fileTransformQueueClientMock.Object,
            _blobStoreMock.Object,
            _fileTransformers
        );
    }

    [Fact]
    public async Task Run_FileNotFound_ExitsSilently()
    {
        // Arrange
        var message = new FileTransformQueueMessage { FileId = "nonexistent-file" };

        // Act
        await _function.Run(message);

        // Assert
        LoggerMock.VerifyLogger(LogLevel.Warning, $"File with id: {message.FileId} not found in MeshFiles table.");

        Assert.Equal(0, DbContext.MeshFiles.Count());
        _blobStoreMock.Verify(x => x.DownloadAsync(It.IsAny<MeshFile>()), Times.Never);
    }

    [Theory]
    [InlineData(MeshFileStatus.Discovered)]
    [InlineData(MeshFileStatus.Extracting)]
    [InlineData(MeshFileStatus.Transforming)]
    [InlineData(MeshFileStatus.Transformed)]
    [InlineData(MeshFileStatus.FailedExtract)]
    [InlineData(MeshFileStatus.FailedTransform)]
    public async Task Run_FileStatusInvalid_ExitsSilently(MeshFileStatus invalidStatus)
    {
        // Arrange
        var file = SaveMeshFile(invalidStatus);
        var message = new FileTransformQueueMessage { FileId = file.FileId };

        // Act
        await _function.Run(message);

        // Assert
        LoggerMock.VerifyLogger(LogLevel.Warning,
            $"File with id: {file.FileId} found in MeshFiles table but is not suitable for transformation. Status: {file.Status}, LastUpdatedUtc: {file.LastUpdatedUtc.ToTimestamp()}.");

        _blobStoreMock.Verify(x => x.DownloadAsync(It.IsAny<MeshFile>()), Times.Never);
        _fileTransformerMock.Verify(c => c.TransformFileAsync(It.IsAny<Stream>(), It.IsAny<MeshFile>()), Times.Never);
    }

    [Fact]
    public async Task Run_FileValidNoTransformersExist_ErrorLoggedAndStatusUpdated()
    {
        // Arrange
        var file = SaveMeshFile();

        _fileTransformers.Clear();

        var message = new FileTransformQueueMessage { FileId = file.FileId };

        // Act
        await _function.Run(message);

        // Assert
        LoggerMock.VerifyLogger(LogLevel.Error,
            $"An exception occurred during file transformation for fileId: {file.FileId}",
            e => e is InvalidOperationException && e.Message.StartsWith("No transformer registered to handle file type: "));

        _blobStoreMock.Verify(x => x.DownloadAsync(file), Times.Never);
        _fileTransformQueueClientMock.Verify(q => q.SendToPoisonQueueAsync(message), Times.Once);

        AssertFileUpdated(file.FileId, MeshFileStatus.FailedTransform);
    }

    [Fact]
    public async Task Run_FileValidMultipleTransformersExist_ErrorLoggedAndStatusUpdated()
    {
        // Arrange
        var file = SaveMeshFile();

        var anotherTransformer = new Mock<IFileTransformer>();
        anotherTransformer.Setup(x => x.CanHandle(MeshFileType.NbssAppointmentEvents)).Returns(true);
        _fileTransformers.Add(anotherTransformer.Object);

        var message = new FileTransformQueueMessage { FileId = file.FileId };

        // Act
        await _function.Run(message);

        // Assert
        LoggerMock.VerifyLogger(LogLevel.Error,
            $"An exception occurred during file transformation for fileId: {file.FileId}",
            e => e is InvalidOperationException && e.Message.StartsWith("Multiple transformers found for file type: "));

        _blobStoreMock.Verify(x => x.DownloadAsync(file), Times.Never);
        _fileTransformQueueClientMock.Verify(q => q.SendToPoisonQueueAsync(message), Times.Once);

        AssertFileUpdated(file.FileId, MeshFileStatus.FailedTransform);
    }

    [Fact]
    public async Task Run_FileHasValidationErrors_ErrorLoggedAndStatusAndValidationErrorsUpdated()
    {
        // Arrange
        var file = SaveMeshFile();

        var expectedStream = new MemoryStream();
        _blobStoreMock.Setup(m => m.DownloadAsync(file)).ReturnsAsync(expectedStream);

        var validationErrors = new List<ValidationError>
        {
            new() { Scope = ValidationErrorScope.Record, Code = "NBSSAPPT001", Error = "error message", Field = "field", RowNumber = 1 },
            new() { Scope = ValidationErrorScope.Header, Code = "NBSSAPPT002", Error = "error message 2", Field = "field 2" }
        };

        _fileTransformerMock.Setup(c => c.TransformFileAsync(expectedStream, file))
            .ReturnsAsync(validationErrors);

        var message = new FileTransformQueueMessage { FileId = file.FileId };

        // Act
        await _function.Run(message);

        // Assert
        LoggerMock.VerifyLogger(LogLevel.Error,
            $"An exception occurred during file transformation for fileId: {file.FileId}",
            e => e is InvalidOperationException && e.Message.StartsWith("Validation errors encountered"));

        _blobStoreMock.Verify(x => x.DownloadAsync(file), Times.Once);
        _fileTransformQueueClientMock.Verify(q => q.SendToPoisonQueueAsync(message), Times.Once);

        var updatedFile = AssertFileUpdated(file.FileId, MeshFileStatus.FailedTransform);
        var savedValidationErrors = DeserializeValidationErrorsFromMeshFile(updatedFile);
        Assert.Equal(validationErrors, savedValidationErrors, new ValidationErrorComparer());
    }

    [Theory]
    [InlineData(MeshFileStatus.Extracted, 0)]
    [InlineData(MeshFileStatus.Transforming, 13)]
    public async Task Run_FileValid_FileTransformedAndStatusUpdated(MeshFileStatus validStatus, int hoursOld)
    {
        // Arrange
        var file = SaveMeshFile(validStatus, hoursOld);

        var expectedStream = new MemoryStream();
        _blobStoreMock.Setup(m => m.DownloadAsync(file)).ReturnsAsync(expectedStream);

        _fileTransformerMock.Setup(c => c.TransformFileAsync(expectedStream, file))
            .ReturnsAsync(new List<ValidationError>());

        var message = new FileTransformQueueMessage { FileId = file.FileId };

        // Act
        await _function.Run(message);

        // Assert
        LoggerMock.VerifyNoLogs(LogLevel.Warning);
        _blobStoreMock.Verify(x => x.DownloadAsync(file), Times.Once);
        _fileTransformerMock.Verify(x => x.TransformFileAsync(expectedStream, file), Times.Once);
        AssertFileUpdated(file.FileId, MeshFileStatus.Transformed);
    }

    private static readonly JsonSerializerOptions ValidationErrorJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static List<ValidationError> DeserializeValidationErrorsFromMeshFile(MeshFile file)
    {
        return JsonSerializer.Deserialize<List<ValidationError>>(
            file.ValidationErrors ?? "[]",
            ValidationErrorJsonOptions
        ) ?? [];
    }

    private class ValidationErrorComparer : IEqualityComparer<ValidationError>
    {
        public bool Equals(ValidationError? x, ValidationError? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.Field == y.Field &&
                x.Error == y.Error &&
                x.Code == y.Code &&
                x.RowNumber == y.RowNumber;
        }

        public int GetHashCode(ValidationError obj)
        {
            return HashCode.Combine(obj.Field, obj.Error, obj.Code, obj.RowNumber);
        }
    }
}
