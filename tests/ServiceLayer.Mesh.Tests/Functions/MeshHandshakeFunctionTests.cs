using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Functions;

namespace ServiceLayer.Mesh.Tests.Functions;

public class MeshHandshakeFunctionTests
{
    private readonly Mock<ILogger<MeshHandshakeFunction>> _loggerMock;
    private readonly Mock<IMeshOperationService> _meshOperationServiceMock;
    private readonly Mock<IMeshHandshakeFunctionConfiguration> _configurationMock;
    private readonly MeshHandshakeFunction _function;
    private readonly TimerInfo _timerInfo;
    private const string TestMailboxId = "test-mailbox-123";

    public MeshHandshakeFunctionTests()
    {
        _loggerMock = new Mock<ILogger<MeshHandshakeFunction>>();
        _meshOperationServiceMock = new Mock<IMeshOperationService>();
        _configurationMock = new Mock<IMeshHandshakeFunctionConfiguration>();
        _timerInfo = new TimerInfo();

        _configurationMock.Setup(c => c.NbssMeshMailboxId).Returns(TestMailboxId);
        _function = new MeshHandshakeFunction(
            _loggerMock.Object,
            _meshOperationServiceMock.Object,
            _configurationMock.Object
        );
    }

    [Fact]
    public async Task Run_SuccessfulHandshake_LogsSuccessAndCompletion()
    {
        // Arrange
        var successfulResponse = new MeshResponse<HandshakeResponse>
        {
            IsSuccessful = true,
            Response = new HandshakeResponse { MailboxId = TestMailboxId }
        };
        _meshOperationServiceMock
            .Setup(s => s.MeshHandshakeAsync(TestMailboxId))
            .ReturnsAsync(successfulResponse);

        // Act
        await _function.Run(_timerInfo);

        // Assert
        _meshOperationServiceMock.Verify(s => s.MeshHandshakeAsync(TestMailboxId), Times.Once());
        VerifyLogMessage(LogLevel.Information, "MeshHandshakeFunction started at");
        VerifyLogMessage(LogLevel.Information, "Mesh handshake completed successfully for mailbox");
    }

    [Fact]
    public async Task Run_FailedHandshake_LogsWarningAndCompletion()
    {
        // Arrange
        var failedResponse = new MeshResponse<HandshakeResponse>
        {
            IsSuccessful = false,
            Error = new APIErrorResponse
            {
                ErrorDescription = "Authentication failed"
            }
        };
        _meshOperationServiceMock
            .Setup(s => s.MeshHandshakeAsync(TestMailboxId))
            .ReturnsAsync(failedResponse);

        // Act
        await _function.Run(_timerInfo);

        // Assert
        _meshOperationServiceMock.Verify(s => s.MeshHandshakeAsync(TestMailboxId), Times.Once());
        VerifyLogMessage(LogLevel.Information, "MeshHandshakeFunction started at");
        VerifyLogMessage(LogLevel.Warning, "Mesh handshake failed");
    }

    [Fact]
    public async Task Run_ExceptionThrown_LogsErrorAndCompletion()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Connection failed");
        _meshOperationServiceMock
            .Setup(s => s.MeshHandshakeAsync(TestMailboxId))
            .ThrowsAsync(expectedException);

        // Act
        await _function.Run(_timerInfo);

        // Assert
        _meshOperationServiceMock.Verify(s => s.MeshHandshakeAsync(TestMailboxId), Times.Once());
        VerifyLogMessage(LogLevel.Information, "MeshHandshakeFunction started at");
        VerifyLogMessage(LogLevel.Error, "An error occurred during mesh handshake");
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private void VerifyLogMessage(LogLevel level, string expectedMessage)
    {
        _loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
