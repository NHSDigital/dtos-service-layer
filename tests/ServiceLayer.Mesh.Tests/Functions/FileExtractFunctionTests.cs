using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Moq;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Functions;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Storage;
using ServiceLayer.TestUtilities;

namespace ServiceLayer.Mesh.Tests.Functions;

public class FileExtractFunctionTests : FunctionTestBase<FileExtractFunction>
{
    private readonly Mock<IMeshInboxService> _meshInboxServiceMock;
    private readonly Mock<IFileTransformQueueClient> _fileTransformQueueClientMock;
    private readonly Mock<IFileExtractQueueClient> _fileExtractQueueClientMock;
    private readonly Mock<IMeshFilesBlobStore> _blobStoreMock;
    private readonly FileExtractFunction _function;

    public FileExtractFunctionTests()
    {
        _meshInboxServiceMock = new Mock<IMeshInboxService>();
        _fileExtractQueueClientMock = new Mock<IFileExtractQueueClient>();
        _fileTransformQueueClientMock = new Mock<IFileTransformQueueClient>();
        _blobStoreMock = new Mock<IMeshFilesBlobStore>();

        _function = new FileExtractFunction(
            LoggerMock.Object,
            _meshInboxServiceMock.Object,
            DbContext,
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
        LoggerMock.VerifyLogger(LogLevel.Warning,
            $"File with id: {message.FileId} not found in MeshFiles table.");

        Assert.Equal(0, DbContext.MeshFiles.Count());
        _meshInboxServiceMock.Verify(x => x.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _blobStoreMock.Verify(x => x.UploadAsync(It.IsAny<MeshFile>(), It.IsAny<byte[]>()), Times.Never);
        _fileTransformQueueClientMock.Verify(x => x.EnqueueFileTransformAsync(It.IsAny<MeshFile>()), Times.Never);
        _fileTransformQueueClientMock.Verify(x => x.SendToPoisonQueueAsync(It.IsAny<FileTransformQueueMessage>()), Times.Never);
    }

    [Theory]
    [InlineData(MeshFileStatus.Extracted)]
    [InlineData(MeshFileStatus.Extracting)]
    [InlineData(MeshFileStatus.Transforming)]
    [InlineData(MeshFileStatus.Transformed)]
    [InlineData(MeshFileStatus.FailedExtract)]
    [InlineData(MeshFileStatus.FailedTransform)]
    public async Task Run_FileStatusInvalid_ExitsSilently(MeshFileStatus invalidStatus)
    {
        // Arrange
        var file = SaveMeshFile(invalidStatus);
        var message = new FileExtractQueueMessage { FileId = file.FileId };

        // Act
        await _function.Run(message);

        // Assert
        LoggerMock.VerifyLogger(LogLevel.Warning,
            $"File with id: {message.FileId} found in MeshFiles table but is not suitable for extraction. Status: {file.Status}, LastUpdatedUtc: {file.LastUpdatedUtc.ToTimestamp()}.");

        _meshInboxServiceMock.Verify(x => x.GetHeadMessageByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _blobStoreMock.Verify(x => x.UploadAsync(It.IsAny<MeshFile>(), It.IsAny<byte[]>()), Times.Never);
        _fileTransformQueueClientMock.Verify(x => x.EnqueueFileTransformAsync(It.IsAny<MeshFile>()), Times.Never);
        _fileTransformQueueClientMock.Verify(x => x.SendToPoisonQueueAsync(It.IsAny<FileTransformQueueMessage>()), Times.Never);
    }

    [Theory]
    [InlineData(MeshFileStatus.Discovered, 0)]
    [InlineData(MeshFileStatus.Extracting, 13)]
    public async Task Run_FileValid_FileUploadedToBlobAndAcknowledgedAndEnqueued(MeshFileStatus validStatus, int hoursOld)
    {
        // Arrange
        var file = SaveMeshFile(validStatus, hoursOld);

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

        var updatedFile = AssertFileUpdated(file.FileId, MeshFileStatus.Extracted, FileEventSource.ExtractFunction);
        Assert.Equal(blobPath, updatedFile.BlobPath);
    }

    [Fact]
    public async Task Run_GetMessageFails_ErrorLoggedAndFileSentToPoisonQueue()
    {
        // Arrange
        var file = SaveMeshFile(MeshFileStatus.Discovered);

        _meshInboxServiceMock.Setup(s => s.GetMessageByIdAsync(file.MailboxId, file.FileId))
            .ReturnsAsync(new MeshResponse<GetMessageResponse>
            {
                IsSuccessful = false,
                Error = new APIErrorResponse
                {
                    ErrorCode = "code",
                    ErrorDescription = "description",
                    ErrorEvent = "event"
                }
            });

        var message = new FileExtractQueueMessage { FileId = file.FileId };

        // Act
        await _function.Run(message);

        // Assert
        LoggerMock.VerifyLogger(LogLevel.Error,
            $"An exception occurred during file extraction for fileId: {file.FileId}",
        e => e is InvalidOperationException && e.Message.StartsWith("Mesh extraction failed: [ ErrorEvent: event, ErrorCode: code, ErrorDescription: description ]")
            );

        _blobStoreMock.Verify(b => b.UploadAsync(It.IsAny<MeshFile>(), It.IsAny<byte[]>()), Times.Never);
        _meshInboxServiceMock.Verify(m => m.AcknowledgeMessageByIdAsync(file.MailboxId, file.FileId), Times.Never);
        _fileTransformQueueClientMock.Verify(q => q.EnqueueFileTransformAsync(It.IsAny<MeshFile>()), Times.Never);
        _fileExtractQueueClientMock.Verify(q => q.SendToPoisonQueueAsync(message), Times.Once);
        var updatedFile = AssertFileUpdated(file.FileId, MeshFileStatus.FailedExtract, FileEventSource.ExtractFunction);
        Assert.Null(updatedFile.BlobPath);
    }

    [Fact]
    public async Task Run_AcknowledgeMessageFails_WarningLoggedAndProcessingContinuesAsNormal()
    {
        // Arrange
        var file = SaveMeshFile(MeshFileStatus.Discovered);

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
        _blobStoreMock.Setup(s => s.UploadAsync(file, content)).ReturnsAsync("directory/fileName");
        _meshInboxServiceMock.Setup(s => s.AcknowledgeMessageByIdAsync(file.MailboxId, file.FileId))
            .ReturnsAsync(new MeshResponse<AcknowledgeMessageResponse>
            {
                IsSuccessful = false,
                Error = new APIErrorResponse
                {
                    ErrorCode = "code",
                    ErrorDescription = "description",
                    ErrorEvent = "event"
                }
            });

        var message = new FileExtractQueueMessage { FileId = file.FileId };

        // Act
        await _function.Run(message);

        // Assert
        LoggerMock.VerifyLogger(LogLevel.Warning,
            "Mesh acknowledgement failed: [ ErrorEvent: event, ErrorCode: code, ErrorDescription: description ].\nThis is not a fatal error so processing will continue.");

        _blobStoreMock.Verify(b => b.UploadAsync(file, content), Times.Once);
        _meshInboxServiceMock.Verify(m => m.AcknowledgeMessageByIdAsync(file.MailboxId, file.FileId), Times.Once);
        _fileTransformQueueClientMock.Verify(q => q.EnqueueFileTransformAsync(file), Times.Once);
        _fileExtractQueueClientMock.Verify(q => q.SendToPoisonQueueAsync(message), Times.Never);
        var updatedFile = AssertFileUpdated(file.FileId, MeshFileStatus.Extracted, FileEventSource.ExtractFunction);
        Assert.Equal(blobPath, updatedFile.BlobPath);
    }
}
