using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Functions;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Models;
using ServiceLayer.Mesh.Configuration;
using Xunit;

namespace ServiceLayer.Mesh.Tests.Functions;

public class FileRetryFunctionTests
{
    private readonly Mock<ILogger<FileRetryFunction>> _loggerMock;
    private readonly Mock<IMeshInboxService> _meshInboxServiceMock;
    private readonly Mock<IFileExtractQueueClient> _fileExtractQueueClientMock;
    private readonly Mock<IFileTransformQueueClient> _fileTransformQueueClientMock;
    private readonly Mock<IFileRetryFunctionConfiguration> _configuration;
    private readonly ServiceLayerDbContext _dbContext;
    private readonly FileRetryFunction _function;

    public FileRetryFunctionTests()
    {
        _loggerMock = new Mock<ILogger<FileRetryFunction>>();
        _meshInboxServiceMock = new Mock<IMeshInboxService>();
        _fileExtractQueueClientMock = new Mock<IFileExtractQueueClient>();
        _fileTransformQueueClientMock = new Mock<IFileTransformQueueClient>();
        _configuration = new Mock<IFileRetryFunctionConfiguration>();

        var options = new DbContextOptionsBuilder<ServiceLayerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new ServiceLayerDbContext(options);

        _configuration.Setup(c => c.StaleHours).Returns("12");

        _function = new FileRetryFunction(
            _loggerMock.Object,
            _meshInboxServiceMock.Object,
            _dbContext,
            _fileExtractQueueClientMock.Object,
            _fileTransformQueueClientMock.Object,
            _configuration.Object
        );
    }

    [Fact]
    public async Task Run_EnqueuesDiscoveredFileOlderThan12Hours()
    {
        // Arrange
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = "file-1",
            Status = MeshFileStatus.Discovered,
            LastUpdatedUtc = DateTime.UtcNow.AddHours(-13)
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        // Act
        await _function.Run(null);

        // Assert
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.Is<MeshFile>(f => f.FileId == "file-1")), Times.Once);

        var updatedFile = await _dbContext.MeshFiles.FindAsync("file-1");
        Assert.True(updatedFile!.LastUpdatedUtc > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task Run_SkipsFreshDiscoveredFile()
    {
        // Arrange
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = "file-2",
            Status = MeshFileStatus.Discovered,
            LastUpdatedUtc = DateTime.UtcNow.AddHours(-1)
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        // Act
        await _function.Run(null);

        // Assert
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Never);
    }

    [Fact]
    public async Task Run_EnqueuesExtractingFileOlderThan12Hours()
    {
        // Arrange
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = "file-3",
            Status = MeshFileStatus.Extracting,
            LastUpdatedUtc = DateTime.UtcNow.AddHours(-13)
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        // Act
        await _function.Run(null);

        // Assert
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.Is<MeshFile>(f => f.FileId == "file-3")), Times.Once);
    }

    [Fact]
    public async Task Run_UpdatesTimestampForExtractedFileButDoesNotEnqueue()
    {
        // Arrange
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = "file-4",
            Status = MeshFileStatus.Extracted,
            LastUpdatedUtc = DateTime.UtcNow.AddHours(-13)
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        // Act
        await _function.Run(null);

        // Assert
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Never);

        var updated = await _dbContext.MeshFiles.FindAsync("file-4");
        Assert.True(updated!.LastUpdatedUtc > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task Run_IgnoresFilesInOtherStatuses()
    {
        // Arrange
        var file = new MeshFile
        {
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            FileId = "file-5",
            Status = MeshFileStatus.Transformed,
            LastUpdatedUtc = DateTime.UtcNow.AddHours(-20)
        };
        _dbContext.MeshFiles.Add(file);
        await _dbContext.SaveChangesAsync();

        // Act
        await _function.Run(null);

        // Assert
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Never);
    }

    [Fact]
    public async Task Run_ProcessesMultipleEligibleFiles()
    {
        // Arrange
        var files = new[]
        {
            new MeshFile
            {
                FileType = MeshFileType.NbssAppointmentEvents,
                MailboxId = "test-mailbox",
                FileId = "file-6",
                Status = MeshFileStatus.Discovered,
                LastUpdatedUtc = DateTime.UtcNow.AddHours(-13)
            },
            new MeshFile
            {
                FileType = MeshFileType.NbssAppointmentEvents,
                MailboxId = "test-mailbox",
                FileId = "file-7",
                Status = MeshFileStatus.Extracted,
                LastUpdatedUtc = DateTime.UtcNow.AddHours(-13)
            },
            new MeshFile
            {
                FileType = MeshFileType.NbssAppointmentEvents,
                MailboxId = "test-mailbox",
                FileId = "file-8",
                Status = MeshFileStatus.Transforming,
                LastUpdatedUtc = DateTime.UtcNow.AddHours(-13)
            }
        };

        _dbContext.MeshFiles.AddRange(files);
        await _dbContext.SaveChangesAsync();

        // Act
        await _function.Run(null);

        // Assert
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.Is<MeshFile>(f => f.FileId == "file-6")), Times.Once);
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Once);

        foreach (var fileId in new[] { "file-6", "file-7", "file-8" })
        {
            var updated = await _dbContext.MeshFiles.FindAsync(fileId);
            Assert.True(updated!.LastUpdatedUtc > DateTime.UtcNow.AddMinutes(-1));
        }
    }
}
