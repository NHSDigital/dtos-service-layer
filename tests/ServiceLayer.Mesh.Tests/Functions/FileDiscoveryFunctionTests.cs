using Microsoft.Azure.Functions.Worker;
using Moq;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Functions;
using ServiceLayer.Mesh.Messaging;

namespace ServiceLayer.Mesh.Tests.Functions;

public class FileDiscoveryFunctionTests : FunctionTestBase<FileDiscoveryFunction>
{
    private readonly Mock<IMeshInboxService> _meshInboxServiceMock;
    private readonly Mock<IFileExtractQueueClient> _queueClientMock;
    private readonly FileDiscoveryFunction _function;

    public FileDiscoveryFunctionTests()
    {
        _meshInboxServiceMock = new Mock<IMeshInboxService>();
        _queueClientMock = new Mock<IFileExtractQueueClient>();

        var functionConfiguration = new Mock<IFileDiscoveryFunctionConfiguration>();
        functionConfiguration.Setup(c => c.NbssMeshMailboxId).Returns("test-mailbox");

        _function = new FileDiscoveryFunction(
            LoggerMock.Object,
            functionConfiguration.Object,
            _meshInboxServiceMock.Object,
            DbContext,
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
                Response = new CheckInboxResponse { Messages = [testMessageId] }
            });

        // Act
        await _function.Run(new TimerInfo());

        // Assert
        var meshFile = AssertFileUpdated(testMessageId, MeshFileStatus.Discovered);
        Assert.Equal("test-mailbox", meshFile.MailboxId);

        // TODO - replace the It.IsAny with a more specific matcher, or use a callback
        _queueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Once);
    }

    [Fact]
    public async Task Run_DoesNotAddDuplicateMessageOrQueueIt()
    {
        // Arrange
        var existingFile = SaveMeshFile(MeshFileStatus.Discovered);

        _meshInboxServiceMock.Setup(s => s.GetMessagesAsync("test-mailbox"))
            .ReturnsAsync(new MeshResponse<CheckInboxResponse>
            {
                Response = new CheckInboxResponse { Messages = [existingFile.FileId] }
            });

        // Act
        await _function.Run(new TimerInfo());

        // Assert
        var count = DbContext.MeshFiles.Count(f => f.FileId == existingFile.FileId);
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
        await _function.Run(new TimerInfo());

        // Assert
        Assert.Empty(DbContext.MeshFiles);
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
        await _function.Run(new TimerInfo());

        // Assert
        foreach (var id in messageIds)
        {
            var savedFile = AssertFileUpdated(id, MeshFileStatus.Discovered);
            Assert.Equal("test-mailbox", savedFile.MailboxId);
        }

        // TODO - replace the It.IsAny with more specific matcher, or use a callback to capture the arguments and check the file IDs
        _queueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Exactly(messageIds.Length));
    }
}
