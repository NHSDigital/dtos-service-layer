using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Functions;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Models;
using ServiceLayer.Mesh.Storage;

namespace ServiceLayer.Mesh.Tests.Functions;

public class FileTransformFunctionTests
{
    private readonly Mock<ILogger<FileTransformFunction>> _loggerMock;
    private readonly Mock<IMeshFilesBlobStore> _blobStoreMock;
    private readonly ServiceLayerDbContext _dbContext;
    private readonly FileTransformFunction _function;

    public FileTransformFunctionTests()
    {
        _loggerMock = new Mock<ILogger<FileTransformFunction>>();
        _blobStoreMock = new Mock<IMeshFilesBlobStore>();

        var options = new DbContextOptionsBuilder<ServiceLayerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new ServiceLayerDbContext(options);

        _function = new FileTransformFunction(
            _loggerMock.Object,
            _dbContext,
            _blobStoreMock.Object
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

    [Fact]
    public async Task Run_FileStatusInvalid_ExitsSilently()
    {
        // Arrange
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = "file-1",
            Status = MeshFileStatus.FailedExtract, // Not eligible
            LastUpdatedUtc = DateTime.UtcNow
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
                It.Is<It.IsAnyType>((v, t) => v.ToString() == $"File with id: {message.FileId} found in MeshFiles table but is not suitable for transformation. Status: {file.Status}"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        var fileFromDb = await _dbContext.MeshFiles.SingleOrDefaultAsync(x => x.FileId == file.FileId);
        Assert.Equal(MeshFileStatus.FailedExtract, fileFromDb?.Status);
        _blobStoreMock.Verify(x => x.UploadAsync(It.IsAny<MeshFile>(), It.IsAny<byte[]>()), Times.Never);
    }
}
