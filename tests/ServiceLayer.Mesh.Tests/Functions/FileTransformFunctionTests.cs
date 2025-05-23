using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Data;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Functions;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Storage;

namespace ServiceLayer.Mesh.Tests.Functions;

public class FileTransformFunctionTests
{
    private readonly Mock<ILogger<FileTransformFunction>> _loggerMock = new();
    private readonly Mock<IMeshFilesBlobStore> _blobStoreMock = new();
    private readonly Mock<IFileTransformFunctionConfiguration> _configuration = new();
    private readonly Mock<IFileTransformQueueClient> _fileTransformQueueClientMock = new();
    private readonly ServiceLayerDbContext _dbContext;
    private readonly FileTransformFunction _function;
    private readonly List<IFileTransformer> _fileTransformers = new();
    private readonly Mock<IFileTransformer> _fileTransformerMock = new();

    public FileTransformFunctionTests()
    {
        var options = new DbContextOptionsBuilder<ServiceLayerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new ServiceLayerDbContext(options);

        _configuration.Setup(c => c.StaleHours).Returns(12);

        _fileTransformerMock.Setup(c => c.CanHandle(MeshFileType.NbssAppointmentEvents)).Returns(true);
        _fileTransformers.Add(_fileTransformerMock.Object);

        _function = new FileTransformFunction(
            _loggerMock.Object,
            _configuration.Object,
            _dbContext,
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
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == $"File with id: {message.FileId} not found in MeshFiles table."),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);

        Assert.Equal(0, _dbContext.MeshFiles.Count());
        _blobStoreMock.Verify(x => x.DownloadAsync(It.IsAny<MeshFile>()), Times.Never);
    }

    [Theory]
    [InlineData(MeshFileStatus.Discovered)]
    [InlineData(MeshFileStatus.Extracting)]
    [InlineData(MeshFileStatus.Transforming)]
    [InlineData(MeshFileStatus.Transformed)]
    [InlineData(MeshFileStatus.FailedExtract)]
    [InlineData(MeshFileStatus.FailedTransform)]
    public async Task Run_FileStatusInvalid_ExitsSilently(MeshFileStatus invalidStatus )
    {
        // Arrange
        var originalLastUpdatedUtc = DateTime.UtcNow.AddHours(-1);
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = "file-1",
            Status = invalidStatus,
            LastUpdatedUtc = originalLastUpdatedUtc,
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var message = new FileTransformQueueMessage { FileId = "file-1" };

        // Act
        await _function.Run(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == $"File with id: {message.FileId} found in MeshFiles table but is not suitable for transformation. Status: {file.Status}, LastUpdatedUtc: {file.LastUpdatedUtc.ToTimestamp()}."),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);

        _blobStoreMock.Verify(x => x.DownloadAsync(It.IsAny<MeshFile>()), Times.Never);
        _fileTransformerMock.Verify(c => c.TransformFileAsync(It.IsAny<Stream>(), It.IsAny<MeshFile>()), Times.Never);
    }

    [Fact]
    public async Task Run_FileValidNoTransformersExist_ErrorLoggedAndStatusUpdated()
    {
        // Arrange
        var fileId = "file-4";
        DateTime originalLastUpdatedUtc = DateTime.UtcNow.AddHours(-1);
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = fileId,
            Status = MeshFileStatus.Extracted,
            LastUpdatedUtc = originalLastUpdatedUtc,
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var expectedStream = new MemoryStream();
        _blobStoreMock.Setup(m => m.DownloadAsync(file)).ReturnsAsync(expectedStream);

        _fileTransformers.Clear();

        var message = new FileTransformQueueMessage { FileId = fileId };

        // Act
        await _function.Run(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == $"An exception occurred during file transformation for fileId: {fileId}"),
                It.Is<InvalidOperationException>(e => e.Message.StartsWith("No transformer registered to handle file type: ")),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        _blobStoreMock.Verify(x => x.DownloadAsync(file), Times.Once);
        _fileTransformQueueClientMock.Verify(q => q.SendToPoisonQueueAsync(message), Times.Once);

        var updatedFile = await _dbContext.MeshFiles.SingleOrDefaultAsync(x => x.FileId == file.FileId);
        Assert.Equal(MeshFileStatus.FailedTransform, updatedFile?.Status);
        Assert.True(updatedFile?.LastUpdatedUtc > originalLastUpdatedUtc);
    }

    [Fact]
    public async Task Run_FileValidMultipleTransformersExist_ErrorLoggedAndStatusUpdated()
    {
        // Arrange
        var fileId = "file-4";
        DateTime originalLastUpdatedUtc = DateTime.UtcNow.AddHours(-1);
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = fileId,
            Status = MeshFileStatus.Extracted,
            LastUpdatedUtc = originalLastUpdatedUtc,
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var expectedStream = new MemoryStream();
        _blobStoreMock.Setup(m => m.DownloadAsync(file)).ReturnsAsync(expectedStream);

        var anotherTransformer = new Mock<IFileTransformer>();
        anotherTransformer.Setup(x => x.CanHandle(MeshFileType.NbssAppointmentEvents)).Returns(true);
        _fileTransformers.Add(anotherTransformer.Object);

        var message = new FileTransformQueueMessage { FileId = fileId };

        // Act
        await _function.Run(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == $"An exception occurred during file transformation for fileId: {fileId}"),
                It.Is<InvalidOperationException>(e => e.Message.StartsWith("Multiple transformers found for file type: ")),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        _blobStoreMock.Verify(x => x.DownloadAsync(file), Times.Once);
        _fileTransformQueueClientMock.Verify(q => q.SendToPoisonQueueAsync(message), Times.Once);

        var updatedFile = await _dbContext.MeshFiles.SingleOrDefaultAsync(x => x.FileId == file.FileId);
        Assert.Equal(MeshFileStatus.FailedTransform, updatedFile?.Status);
        Assert.True(updatedFile?.LastUpdatedUtc > originalLastUpdatedUtc);
    }

    [Fact]
    public async Task Run_FileHasValidationErrors_ErrorLoggedAndStatusAndValidationErrorsUpdated()
    {
        // Arrange
        var fileId = "file-4";
        DateTime originalLastUpdatedUtc = DateTime.UtcNow.AddHours(-1);
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = fileId,
            Status = MeshFileStatus.Extracted,
            LastUpdatedUtc = originalLastUpdatedUtc,
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var expectedStream = new MemoryStream();
        _blobStoreMock.Setup(m => m.DownloadAsync(file)).ReturnsAsync(expectedStream);

        var validationErrors = new List<ValidationError>
        {
            new(){ Code = "NBSSAPPT001", Error = "error message", Field = "field", RowNumber = 1},
            new(){ Code = "NBSSAPPT002", Error = "error message 2", Field = "field 2"}
        };

        _fileTransformerMock.Setup(c => c.TransformFileAsync(expectedStream, file))
            .ReturnsAsync(validationErrors);

        var message = new FileTransformQueueMessage { FileId = fileId };

        // Act
        await _function.Run(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == $"An exception occurred during file transformation for fileId: {fileId}"),
                It.Is<InvalidOperationException>(e => e.Message.StartsWith("Validation errors encountered")),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        _blobStoreMock.Verify(x => x.DownloadAsync(file), Times.Once);
        _fileTransformQueueClientMock.Verify(q => q.SendToPoisonQueueAsync(message), Times.Once);

        var updatedFile = await _dbContext.MeshFiles.SingleOrDefaultAsync(x => x.FileId == file.FileId);
        Assert.Equal(MeshFileStatus.FailedTransform, updatedFile?.Status);
        Assert.True(updatedFile?.LastUpdatedUtc > originalLastUpdatedUtc);

        var savedValidationErrors = ValidationTestHelpers.GetValidationErrorsFromMeshFile(updatedFile);

        Assert.Equal(validationErrors, savedValidationErrors, new ValidationErrorComparer());
    }

    [Theory]
    [InlineData(MeshFileStatus.Extracted, 0)]
    [InlineData(MeshFileStatus.Transforming, 13)]
    public async Task Run_FileValid_FileTransformedAndStatusUpdated(MeshFileStatus validStatus, int hoursOld)
    {
        // Arrange
        DateTime originalLastUpdatedUtc = DateTime.UtcNow.AddHours(-hoursOld);
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = "file-1",
            Status = validStatus,
            LastUpdatedUtc = originalLastUpdatedUtc,
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var expectedStream = new MemoryStream();
        _blobStoreMock.Setup(m => m.DownloadAsync(file)).ReturnsAsync(expectedStream);

        _fileTransformerMock.Setup(c => c.TransformFileAsync(expectedStream, file))
            .ReturnsAsync(new List<ValidationError>());

        var message = new FileTransformQueueMessage { FileId = "file-1" };

        // Act
        await _function.Run(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Never);
        _blobStoreMock.Verify(x => x.DownloadAsync(file), Times.Once);
        _fileTransformerMock.Verify(x => x.TransformFileAsync(expectedStream, file), Times.Once);

        var updatedFile = await _dbContext.MeshFiles.SingleOrDefaultAsync(x => x.FileId == file.FileId);
        Assert.Equal(MeshFileStatus.Transformed, updatedFile?.Status);
        Assert.True(updatedFile?.LastUpdatedUtc > originalLastUpdatedUtc);
    }
}

public class ValidationErrorComparer : IEqualityComparer<ValidationError>
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

public static class ValidationTestHelpers
{
    private static readonly JsonSerializerOptions ValidationErrorJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static List<ValidationError> GetValidationErrorsFromMeshFile(MeshFile file)
    {
        return JsonSerializer.Deserialize<List<ValidationError>>(
            file.ValidationErrors ?? "[]",
            ValidationErrorJsonOptions
        ) ?? [];
    }
}
