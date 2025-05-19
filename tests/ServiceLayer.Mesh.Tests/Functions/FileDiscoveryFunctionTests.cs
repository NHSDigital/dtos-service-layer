using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;
using ServiceLayer.Data;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Functions;
using ServiceLayer.Mesh.Messaging;

namespace ServiceLayer.Mesh.Tests.Functions;

public class FileDiscoveryFunctionTests
{
    private readonly Mock<ILogger<FileDiscoveryFunction>> _loggerMock;
    private readonly Mock<IMeshInboxService> _meshInboxServiceMock;
    private readonly ServiceLayerDbContext _dbContext;
    private readonly Mock<IFileExtractQueueClient> _queueClientMock;
    private readonly FileDiscoveryFunction _function;

    public FileDiscoveryFunctionTests()
    {
        _loggerMock = new Mock<ILogger<FileDiscoveryFunction>>();
        _meshInboxServiceMock = new Mock<IMeshInboxService>();
        _queueClientMock = new Mock<IFileExtractQueueClient>();

        var options = new DbContextOptionsBuilder<ServiceLayerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new ServiceLayerDbContext(options);

        var functionConfiguration = new Mock<IFileDiscoveryFunctionConfiguration>();
        functionConfiguration.Setup(c => c.NbssMeshMailboxId).Returns("test-mailbox");

        _function = new FileDiscoveryFunction(
            _loggerMock.Object,
            functionConfiguration.Object,
            _meshInboxServiceMock.Object,
            _dbContext,
            _queueClientMock.Object
        );
    }

    [Fact]
    public async Task Run_AddsNewMessageToDbAndQueue()
    {
        // Arrange
        var testMessageId = "test-message-123";

        _meshInboxServiceMock.Setup(s => s.GetMessagesAsync("test-mailbox"))
            .ReturnsAsync(new MeshResponse<CheckInboxResponse>
            {
                Response = new CheckInboxResponse { Messages = new[] { testMessageId } }
            });

        // Act
        await _function.Run(null);

        // Assert
        var meshFile = _dbContext.MeshFiles.FirstOrDefault(f => f.FileId == testMessageId);
        Assert.NotNull(meshFile);
        Assert.Equal(MeshFileStatus.Discovered, meshFile.Status);
        Assert.Equal("test-mailbox", meshFile.MailboxId);

        // TODO - replace the It.IsAny with a more specific matcher, or use a callback
        _queueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Once);
    }

    [Fact]
    public async Task Run_DoesNotAddDuplicateMessageOrQueueIt()
    {
        // Arrange
        var duplicateMessageId = "existing-message";
        _dbContext.MeshFiles.Add(new MeshFile
        {
            FileId = duplicateMessageId,
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "test-mailbox",
            Status = MeshFileStatus.Discovered,
            FirstSeenUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        _meshInboxServiceMock.Setup(s => s.GetMessagesAsync("test-mailbox"))
            .ReturnsAsync(new MeshResponse<CheckInboxResponse>
            {
                Response = new CheckInboxResponse { Messages = new[] { duplicateMessageId } }
            });

        // Act
        await _function.Run(null);

        // Assert
        var count = _dbContext.MeshFiles.Count(f => f.FileId == duplicateMessageId);
        Assert.Equal(1, count);

        _queueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Never);
    }

    [Fact]
    public async Task Run_NoMessagesInInbox_DoesNothing()
    {
        // Arrange
        _meshInboxServiceMock.Setup(s => s.GetMessagesAsync("test-mailbox"))
            .ReturnsAsync(new MeshResponse<CheckInboxResponse>
            {
                Response = new CheckInboxResponse { Messages = [] }
            });

        // Act
        await _function.Run(null);

        // Assert
        Assert.Empty(_dbContext.MeshFiles);
        _queueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Never);
    }

    [Fact]
    public async Task Run_MultipleMessagesInInbox_AllAreProcessed()
    {
        // Arrange
        var messageIds = new[] { "msg-1", "msg-2", "msg-3" };

        _meshInboxServiceMock.Setup(s => s.GetMessagesAsync("test-mailbox"))
            .ReturnsAsync(new MeshResponse<CheckInboxResponse>
            {
                Response = new CheckInboxResponse { Messages = messageIds }
            });

        // Act
        await _function.Run(null);

        // Assert
        foreach (var id in messageIds)
        {
            var meshFile = _dbContext.MeshFiles.FirstOrDefault(f => f.FileId == id);
            Assert.NotNull(meshFile);
            Assert.Equal(MeshFileStatus.Discovered, meshFile.Status);
            Assert.Equal("test-mailbox", meshFile.MailboxId);
        }

        // TODO - replace the It.IsAny with more specific matcher, or use a callback to capture the arguments and check the file IDs
        _queueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Exactly(messageIds.Length));
    }
}
