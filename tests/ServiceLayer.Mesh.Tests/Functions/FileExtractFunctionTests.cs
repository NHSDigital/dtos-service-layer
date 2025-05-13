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
        var message = new FileExtractQueueMessage { FileId = "nonexistent-file" };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _function.Run(message));
        Assert.Equal("File not found", exception.Message);
    }

    [Fact]
    public async Task Run_FileInInvalidStatus_ExitsSilently()
    {
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

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _function.Run(message));
        Assert.Equal("File is not in expected status", exception.Message);
    }

    [Fact]
    public async Task Run_FileExtractingButNotTimedOut_ExitsSilently()
    {
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

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _function.Run(message));
        Assert.Equal("File is not in expected status", exception.Message);
    }

    [Fact]
    public async Task Run_FileExtractSuccess_UploadsBlob()
    {
        var fileId = "file-3";
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = fileId,
            Status = MeshFileStatus.Discovered,
            LastUpdatedUtc = DateTime.UtcNow.AddHours(-1)
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        var content = new byte[] { 1, 2, 3 };

        _meshInboxServiceMock.Setup(s => s.GetMessageByIdAsync("test-mailbox", fileId))
            .ReturnsAsync(new MeshResponse<GetMessageResponse>
            {
                IsSuccessful = true,
                Response = new GetMessageResponse
                {
                    FileAttachment = new FileAttachment { Content = content }
                }
            });

        var message = new FileExtractQueueMessage { FileId = fileId };

        await _function.Run(message);

        _blobStoreMock.Verify(b => b.UploadAsync(It.Is<MeshFile>(f => f.FileId == fileId), content), Times.Once);
    }

    [Fact]
    public async Task Run_MeshResponseFails_Throws()
    {
        var fileId = "file-4";
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = fileId,
            Status = MeshFileStatus.Discovered,
            LastUpdatedUtc = DateTime.UtcNow.AddHours(-2)
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        _meshInboxServiceMock.Setup(s => s.GetMessageByIdAsync("test-mailbox", fileId))
            .ReturnsAsync(new MeshResponse<GetMessageResponse>
            {
                IsSuccessful = false,
            });

        var message = new FileExtractQueueMessage { FileId = fileId };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _function.Run(message));
        Assert.Contains("Mesh extraction failed", exception.Message);
    }
}
