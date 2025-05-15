using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Functions;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Models;
using ServiceLayer.Mesh.Storage;

namespace ServiceLayer.Mesh.Tests.Functions;

public class FileExtractFunctionTests
{
    private readonly Mock<ILogger<FileExtractFunction>> _loggerMock;
    private readonly Mock<IMeshInboxService> _meshInboxServiceMock;
    private readonly Mock<IFileTransformQueueClient> _fileTransformQueueClientMock;
    private readonly Mock<IFileExtractQueueClient> _fileExtractQueueClientMock;
    private readonly Mock<IFileExtractFunctionConfiguration> _configurationMock;
    private readonly Mock<IMeshFilesBlobStore> _blobStoreMock;
    private readonly ServiceLayerDbContext _dbContext;
    private readonly FileExtractFunction _function;

    public FileExtractFunctionTests()
    {
        _loggerMock = new Mock<ILogger<FileExtractFunction>>();
        _meshInboxServiceMock = new Mock<IMeshInboxService>();
        _fileExtractQueueClientMock = new Mock<IFileExtractQueueClient>();
        _fileTransformQueueClientMock = new Mock<IFileTransformQueueClient>();
        _blobStoreMock = new Mock<IMeshFilesBlobStore>();
        _configurationMock = new Mock<IFileExtractFunctionConfiguration>();

        var options = new DbContextOptionsBuilder<ServiceLayerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new ServiceLayerDbContext(options);

        var functionConfiguration = new Mock<IFileExtractFunctionConfiguration>();
        functionConfiguration.Setup(c => c.NbssMeshMailboxId).Returns("test-mailbox");

        _function = new FileExtractFunction(
            _loggerMock.Object,
            functionConfiguration.Object,
            _meshInboxServiceMock.Object,
            _dbContext,
            _fileTransformQueueClientMock.Object,
            _fileExtractQueueClientMock.Object,
            _blobStoreMock.Object
        );
    }

    [Fact]
    public async Task Run_FileNotFound_ExitsSilently()
    {
        // Arrange
        var message = new FileExtractQueueMessage { FileId = "nonexistent-file" };

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
        _meshInboxServiceMock.Verify(x => x.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _blobStoreMock.Verify(x => x.UploadAsync(It.IsAny<MeshFile>(), It.IsAny<byte[]>()), Times.Never);
        _fileTransformQueueClientMock.Verify(x => x.EnqueueFileTransformAsync(It.IsAny<MeshFile>()), Times.Never);
        _fileTransformQueueClientMock.Verify(x => x.SendToPoisonQueueAsync(It.IsAny<FileTransformQueueMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_FileStatusInvalid_ExitsSilently()
    {
        // Arrange
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = "file-1",
            Status = MeshFileStatus.Transforming, // Not eligible
            LastUpdatedUtc = DateTime.UtcNow
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var message = new FileExtractQueueMessage { FileId = "file-1" };

        // Act
        await _function.Run(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == $"File with id: {message.FileId} found in MeshFiles table but is not suitable for extraction. Status: {file.Status}, LastUpdatedUtc: {file.LastUpdatedUtc.ToTimestamp()}."),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);

        _meshInboxServiceMock.Verify(x => x.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _blobStoreMock.Verify(x => x.UploadAsync(It.IsAny<MeshFile>(), It.IsAny<byte[]>()), Times.Never);
        _fileTransformQueueClientMock.Verify(x => x.EnqueueFileTransformAsync(It.IsAny<MeshFile>()), Times.Never);
        _fileTransformQueueClientMock.Verify(x => x.SendToPoisonQueueAsync(It.IsAny<FileTransformQueueMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_FileStatusExtractingButNotTimedOut_ExitsSilently()
    {
        // Arrange
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = "file-2",
            Status = MeshFileStatus.Extracting,
            LastUpdatedUtc = DateTime.UtcNow // Not timed out
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var message = new FileExtractQueueMessage { FileId = "file-2" };

        // Act
        await _function.Run(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == $"File with id: {message.FileId} found in MeshFiles table but is not suitable for extraction. Status: {file.Status}, LastUpdatedUtc: {file.LastUpdatedUtc.ToTimestamp()}."),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);

        _meshInboxServiceMock.Verify(x => x.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _blobStoreMock.Verify(x => x.UploadAsync(It.IsAny<MeshFile>(), It.IsAny<byte[]>()), Times.Never);
        _fileTransformQueueClientMock.Verify(x => x.EnqueueFileTransformAsync(It.IsAny<MeshFile>()), Times.Never);
        _fileTransformQueueClientMock.Verify(x => x.SendToPoisonQueueAsync(It.IsAny<FileTransformQueueMessage>()), Times.Never);
    }

    [Fact]
    public async Task Run_FileValid_FileUploadedToBlobAndAcknowledgedAndEnqueued()
    {
        // Arrange
        var originalLastUpdatedUtc = DateTime.UtcNow.AddHours(-1);
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = "file-3",
            Status = MeshFileStatus.Discovered,
            LastUpdatedUtc = originalLastUpdatedUtc
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var content = new byte[] { 1, 2, 3 };
        const string blobPath = "directory/fileName";

        _meshInboxServiceMock.Setup(s => s.GetMessageByIdAsync(file.MailboxId, file.FileId))
            .ReturnsAsync(new MeshResponse<GetMessageResponse>
            {
                IsSuccessful = true,
                Response = new GetMessageResponse
                {
                    FileAttachment = new FileAttachment { Content = content }
                }
            });
        _blobStoreMock.Setup(s => s.UploadAsync(file, content)).ReturnsAsync(blobPath);
        _meshInboxServiceMock.Setup(s => s.AcknowledgeMessageByIdAsync(file.MailboxId, file.FileId))
            .ReturnsAsync(new MeshResponse<AcknowledgeMessageResponse>
            {
                IsSuccessful = true
            });

        var message = new FileExtractQueueMessage { FileId = file.FileId };

        // Act
        await _function.Run(message);

        // Assert
        _blobStoreMock.Verify(b => b.UploadAsync(It.Is<MeshFile>(f => f.FileId == file.FileId), content), Times.Once);
        _meshInboxServiceMock.Verify(m => m.AcknowledgeMessageByIdAsync(file.MailboxId, file.FileId), Times.Once);
        _fileTransformQueueClientMock.Verify(q => q.EnqueueFileTransformAsync(file), Times.Once);
        var updatedFile = _dbContext.MeshFiles.First();
        Assert.Equal(blobPath, updatedFile.BlobPath);
        Assert.Equal(MeshFileStatus.Extracted, updatedFile.Status);
        Assert.True(updatedFile.LastUpdatedUtc > originalLastUpdatedUtc);
    }

    [Fact]
    public async Task Run_GetMessageFails_ErrorLoggedAndFileSentToPoisonQueue()
    {
        // Arrange
        var fileId = "file-4";
        var originalLastUpdatedUtc = DateTime.UtcNow.AddHours(-1);
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = fileId,
            Status = MeshFileStatus.Discovered,
            LastUpdatedUtc = originalLastUpdatedUtc
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        _meshInboxServiceMock.Setup(s => s.GetMessageByIdAsync(file.MailboxId, fileId))
            .ReturnsAsync(new MeshResponse<GetMessageResponse>
            {
                IsSuccessful = false
            });

        var message = new FileExtractQueueMessage { FileId = fileId };

        // Act
        await _function.Run(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == $"An exception occurred during file extraction for fileId: {fileId}"),
                It.Is<InvalidOperationException>(e => e.Message.StartsWith("Mesh extraction failed: ")),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        _blobStoreMock.Verify(b => b.UploadAsync(It.IsAny<MeshFile>(), It.IsAny<byte[]>()), Times.Never);
        _meshInboxServiceMock.Verify(m => m.AcknowledgeMessageByIdAsync(file.MailboxId, file.FileId), Times.Never);
        _fileTransformQueueClientMock.Verify(q => q.EnqueueFileTransformAsync(It.IsAny<MeshFile>()), Times.Never);
        _fileExtractQueueClientMock.Verify(q => q.SendToPoisonQueueAsync(message), Times.Once);
        var updatedFile = _dbContext.MeshFiles.First();
        Assert.Null(updatedFile.BlobPath);
        Assert.Equal(MeshFileStatus.FailedExtract, updatedFile.Status);
        Assert.True(updatedFile.LastUpdatedUtc > originalLastUpdatedUtc);
    }

    [Fact]
    public async Task Run_AcknowledgeMessageFails_WarningLoggedAndProcessingContinuesAsNormal()
    {
        // Arrange
        var fileId = "file-4";
        var originalLastUpdatedUtc = DateTime.UtcNow.AddHours(-1);
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = fileId,
            Status = MeshFileStatus.Discovered,
            LastUpdatedUtc = originalLastUpdatedUtc
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var content = new byte[] { 1, 2, 3 };
        const string blobPath = "directory/fileName";

        _meshInboxServiceMock.Setup(s => s.GetMessageByIdAsync(file.MailboxId, fileId))
            .ReturnsAsync(new MeshResponse<GetMessageResponse>
            {
                IsSuccessful = true,
                Response = new GetMessageResponse
                {
                    FileAttachment = new FileAttachment { Content = content }
                }
            });
        _blobStoreMock.Setup(s => s.UploadAsync(file, content)).ReturnsAsync("directory/fileName");
        _meshInboxServiceMock.Setup(s => s.AcknowledgeMessageByIdAsync(file.MailboxId, file.FileId))
            .ReturnsAsync(new MeshResponse<AcknowledgeMessageResponse>
            {
                IsSuccessful = false
            });

        var message = new FileExtractQueueMessage { FileId = fileId };

        // Act
        await _function.Run(message);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString().StartsWith("Mesh acknowledgement failed: ") &&
                    v.ToString().EndsWith("This is not a fatal error so processing will continue.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        _blobStoreMock.Verify(b => b.UploadAsync(file, content), Times.Once);
        _meshInboxServiceMock.Verify(m => m.AcknowledgeMessageByIdAsync(file.MailboxId, file.FileId), Times.Once);
        _fileTransformQueueClientMock.Verify(q => q.EnqueueFileTransformAsync(file), Times.Once);
        _fileExtractQueueClientMock.Verify(q => q.SendToPoisonQueueAsync(message), Times.Never);
        var updatedFile = _dbContext.MeshFiles.First();
        Assert.Equal(blobPath, updatedFile.BlobPath);
        Assert.Equal(MeshFileStatus.Extracted, updatedFile.Status);
        Assert.True(updatedFile.LastUpdatedUtc > originalLastUpdatedUtc);
    }
}
