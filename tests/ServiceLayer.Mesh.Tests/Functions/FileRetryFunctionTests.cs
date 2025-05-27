using Moq;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.Functions;
using ServiceLayer.Mesh.Messaging;
using ServiceLayer.Mesh.Configuration;

namespace ServiceLayer.Mesh.Tests.Functions;

public class FileRetryFunctionTests : FunctionTestBase<FileRetryFunction>
{
    private readonly Mock<IFileExtractQueueClient> _fileExtractQueueClientMock;
    private readonly Mock<IFileTransformQueueClient> _fileTransformQueueClientMock;
    private readonly FileRetryFunction _function;

    public FileRetryFunctionTests()
    {
        _fileExtractQueueClientMock = new Mock<IFileExtractQueueClient>();
        _fileTransformQueueClientMock = new Mock<IFileTransformQueueClient>();

        var configurationMock = new Mock<IFileRetryFunctionConfiguration>();
        configurationMock.Setup(c => c.StaleHours).Returns(12);

        _function = new FileRetryFunction(
            LoggerMock.Object,
            DbContext,
            _fileExtractQueueClientMock.Object,
            _fileTransformQueueClientMock.Object,
            configurationMock.Object
        );
    }

    [Theory]
    [InlineData(MeshFileStatus.Discovered)]
    [InlineData(MeshFileStatus.Extracting)]
    public async Task Run_EnqueuesDiscoveredOrExtractingFilesOlderThan12Hours(MeshFileStatus testStatus)
    {
        // Arrange
        var file = SaveMeshFile(testStatus, 13);

        // Act
        await _function.Run(null);

        // Assert
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.Is<MeshFile>(f => f.FileId == file.FileId)), Times.Once);
        _fileTransformQueueClientMock.Verify(q => q.EnqueueFileTransformAsync(It.Is<MeshFile>(f => f.FileId == file.FileId)), Times.Never);

        AssertFileUpdated(file.FileId, testStatus);
    }

    [Theory]
    [InlineData(MeshFileStatus.Extracted)]
    [InlineData(MeshFileStatus.Transforming)]
    public async Task Run_EnqueuesExtractedOrTransformingFilesOlderThan12Hours(MeshFileStatus testStatus)
    {
        // Arrange
        var file = SaveMeshFile(testStatus, 13);

        // Act
        await _function.Run(null);

        // Assert
        _fileTransformQueueClientMock.Verify(q => q.EnqueueFileTransformAsync(It.Is<MeshFile>(f => f.FileId == file.FileId)), Times.Once);
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.Is<MeshFile>(f => f.FileId == file.FileId)), Times.Never);

        AssertFileUpdated(file.FileId, testStatus);
    }

    [Theory]
    [InlineData(MeshFileStatus.Discovered)]
    [InlineData(MeshFileStatus.Extracting)]
    [InlineData(MeshFileStatus.Extracted)]
    [InlineData(MeshFileStatus.Transforming)]
    public async Task Run_SkipsFreshFiles(MeshFileStatus testStatus)
    {
        // Arrange
        var file = SaveMeshFile(testStatus);

        // Act
        await _function.Run(null);

        // Assert
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Never);
        _fileTransformQueueClientMock.Verify(q => q.EnqueueFileTransformAsync(It.IsAny<MeshFile>()), Times.Never);

        AssertFileUnchanged(file.FileId, file.Status, file.LastUpdatedUtc);
    }

    [Theory]
    [InlineData(MeshFileStatus.Transformed)]
    [InlineData(MeshFileStatus.FailedExtract)]
    [InlineData(MeshFileStatus.FailedTransform)]
    public async Task Run_IgnoresFilesInOtherStatuses(MeshFileStatus ignoredStatus)
    {
        // Arrange
        SaveMeshFile(ignoredStatus, 20);

        // Act
        await _function.Run(null);

        // Assert
        _fileTransformQueueClientMock.Verify(q => q.EnqueueFileTransformAsync(It.IsAny<MeshFile>()), Times.Never);
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Never);
    }

    [Fact]
    public async Task Run_IfNoFilesFoundDoNothing()
    {
        // Act
        await _function.Run(null);

        // Assert
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.IsAny<MeshFile>()), Times.Never);
        _fileTransformQueueClientMock.Verify(q => q.EnqueueFileTransformAsync(It.IsAny<MeshFile>()), Times.Never);
    }

    [Fact]
    public async Task Run_ProcessesMultipleEligibleFiles()
    {
        // Arrange
        var file1 = SaveMeshFile(MeshFileStatus.Discovered, 13);
        var file2 = SaveMeshFile(MeshFileStatus.Extracted, 13);
        var file3 = SaveMeshFile(MeshFileStatus.Transforming, 13);

        // Act
        await _function.Run(null);

        // Assert
        _fileExtractQueueClientMock.Verify(q => q.EnqueueFileExtractAsync(It.Is<MeshFile>(f => f.FileId == file1.FileId)), Times.Once);
        _fileTransformQueueClientMock.Verify(q => q.EnqueueFileTransformAsync(It.Is<MeshFile>(f => f.FileId == file2.FileId)), Times.Once);
        _fileTransformQueueClientMock.Verify(q => q.EnqueueFileTransformAsync(It.Is<MeshFile>(f => f.FileId == file3.FileId)), Times.Once);

        AssertFileUpdated(file1.FileId, MeshFileStatus.Discovered);
        AssertFileUpdated(file2.FileId, MeshFileStatus.Extracted);
        AssertFileUpdated(file3.FileId, MeshFileStatus.Transforming);
    }
}

