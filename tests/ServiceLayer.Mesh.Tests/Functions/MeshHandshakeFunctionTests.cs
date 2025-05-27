using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using NHS.MESH.Client.Contracts.Services;
using NHS.MESH.Client.Models;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Functions;
using ServiceLayer.TestUtilities;

namespace ServiceLayer.Mesh.Tests.Functions;

public class MeshHandshakeFunctionTests : FunctionTestBase<MeshHandshakeFunction>
{
    private readonly Mock<IMeshOperationService> _meshOperationServiceMock;
    private readonly MeshHandshakeFunction _function;
    private readonly TimerInfo _timerInfo;
    private const string TestMailboxId = "test-mailbox-123";

    public MeshHandshakeFunctionTests()
    {
        _meshOperationServiceMock = new Mock<IMeshOperationService>();
        _timerInfo = new TimerInfo();

        var functionConfigurationMock = new Mock<IMeshHandshakeFunctionConfiguration>();
        functionConfigurationMock.Setup(c => c.NbssMeshMailboxId).Returns(TestMailboxId);

        _function = new MeshHandshakeFunction(
            LoggerMock.Object,
            _meshOperationServiceMock.Object,
            functionConfigurationMock.Object
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
        LoggerMock.VerifyLogger(LogLevel.Information,"MeshHandshakeFunction started.");
        LoggerMock.VerifyLogger(LogLevel.Information, $"Mesh handshake completed successfully for mailbox {TestMailboxId}.");
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
                ErrorCode = "code",
                ErrorEvent = "event",
                ErrorDescription = "desc"
            }
        };
        _meshOperationServiceMock
            .Setup(s => s.MeshHandshakeAsync(TestMailboxId))
            .ReturnsAsync(failedResponse);

        // Act
        await _function.Run(_timerInfo);

        // Assert
        _meshOperationServiceMock.Verify(s => s.MeshHandshakeAsync(TestMailboxId), Times.Once());
        LoggerMock.VerifyLogger(LogLevel.Information,"MeshHandshakeFunction started.");
        LoggerMock.VerifyLogger(LogLevel.Warning,
            $"Mesh handshake failed for mailbox {TestMailboxId}: [ ErrorEvent: event, ErrorCode: code, ErrorDescription: desc ]");
    }

    [Fact]
    public async Task Run_ExceptionThrown_LogsWarningAndCompletion()
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
        LoggerMock.VerifyLogger(LogLevel.Information,"MeshHandshakeFunction started.");
        LoggerMock.VerifyLogger(LogLevel.Warning, $"An error occurred during mesh handshake for mailbox {TestMailboxId}.",
        e => e is InvalidOperationException && e.Message == "Connection failed");
    }
}
