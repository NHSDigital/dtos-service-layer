using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ServiceLayer.Mesh.Functions;
using ServiceLayer.Mesh.Models;
using ServiceLayer.Mesh.Data;
using Microsoft.EntityFrameworkCore;
using NHS.MESH.Client.Contracts.Services;
using Microsoft.Azure.Functions.Worker;
using NHS.MESH.Client.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Azure;

public class DiscoveryFunctionTests
{
    private readonly Mock<ILogger<DiscoveryFunction>> _loggerMock;
    private readonly Mock<IMeshInboxService> _meshInboxServiceMock;
    private readonly ServiceLayerDbContext _dbContext;
    private readonly Mock<QueueClient> _queueClientMock;
    private readonly DiscoveryFunction _function;

    public DiscoveryFunctionTests()
    {
        _loggerMock = new Mock<ILogger<DiscoveryFunction>>();
        _meshInboxServiceMock = new Mock<IMeshInboxService>();
        _queueClientMock = new Mock<QueueClient>();

        var options = new DbContextOptionsBuilder<ServiceLayerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new ServiceLayerDbContext(options);

        Environment.SetEnvironmentVariable("MailboxId", "test-mailbox");
        Environment.SetEnvironmentVariable("QueueUrl", "https://fakestorageaccount.queue.core.windows.net/testqueue");

        _function = new DiscoveryFunction(
            _loggerMock.Object,
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

        _queueClientMock.Verify(q => q.SendMessage(It.IsAny<string>()), Times.Once);
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

        _queueClientMock.Verify(q => q.SendMessage(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Run_NoMessagesInInbox_DoesNothing()
    {
        // Arrange
        _meshInboxServiceMock.Setup(s => s.GetMessagesAsync("test-mailbox"))
            .ReturnsAsync(new MeshResponse<CheckInboxResponse>
            {
                Response = new CheckInboxResponse { Messages = Array.Empty<string>() }
            });

        // Act
        await _function.Run(null);

        // Assert
        Assert.Empty(_dbContext.MeshFiles);
        _queueClientMock.Verify(q => q.SendMessage(It.IsAny<string>()), Times.Never);
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

        _queueClientMock.Verify(q => q.SendMessage(It.IsAny<string>()), Times.Exactly(messageIds.Length));
    }
}
