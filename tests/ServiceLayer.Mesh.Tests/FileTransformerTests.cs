using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Data.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;
using ServiceLayer.TestUtilities;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents;

public class FileTransformerTests
{
    private readonly Mock<IFileParser> _fileParserMock = new();
    private readonly Mock<IValidationRunner> _validationRunnerMock = new();
    private readonly Mock<IStagingPersister> _stagingPersisterMock = new();
    private readonly Mock<ILogger<FileTransformer>> _loggerMock = new();
    private readonly FileTransformer _fileTransformer;
    private readonly MeshFile _testMeshFile;
    private readonly Stream _testStream;
    private readonly ParsedFile parsedFile = new();

    public FileTransformerTests()
    {
        _fileTransformer = new FileTransformer(
            _fileParserMock.Object,
            _validationRunnerMock.Object,
            _stagingPersisterMock.Object,
            _loggerMock.Object);

        _testMeshFile = new MeshFile
        {
            FileId = "test-file-123",
            FileType = MeshFileType.NbssAppointmentEvents,
            MailboxId = "testMailboxId",
            Status = MeshFileStatus.Extracted
        };

        _testStream = new MemoryStream();
    }

    [Fact]
    public void CanHandle_NbssAppointmentEventsFileType_ReturnsTrue()
    {
        // Act
        var result = _fileTransformer.CanHandle(MeshFileType.NbssAppointmentEvents);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_OtherFileType_ReturnsFalse()
    {
        // Act
        var result = _fileTransformer.CanHandle(MeshFileType.Unknown);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TransformFileAsync_ValidFileWithNoValidationErrors_ParsesValidatesAndPersists()
    {
        // Arrange
        var validationErrors = new List<ValidationError>();

        _fileParserMock.Setup(p => p.Parse(_testStream)).Returns(parsedFile);
        _validationRunnerMock.Setup(v => v.Validate(parsedFile)).Returns(validationErrors);

        // Act
        var result = await _fileTransformer.TransformFileAsync(_testStream, _testMeshFile);

        // Assert
        Assert.Empty(result);
        _fileParserMock.Verify(p => p.Parse(_testStream), Times.Once);
        _validationRunnerMock.Verify(v => v.Validate(parsedFile), Times.Once);
        _stagingPersisterMock.Verify(s => s.WriteStagedData(parsedFile, _testMeshFile), Times.Once);
        _loggerMock.VerifyNoLogs(LogLevel.Error);
    }

    [Fact]
    public async Task TransformFileAsync_ValidFileWithValidationErrors_DoesNotPersistData()
    {
        // Arrange
        var validationErrors = new List<ValidationError>
        {
            new() { Code = "TEST001", Error = "Test validation error", Scope = ValidationErrorScope.Record, RowNumber = 1 }
        };

        _fileParserMock.Setup(p => p.Parse(_testStream)).Returns(parsedFile);
        _validationRunnerMock.Setup(v => v.Validate(parsedFile)).Returns(validationErrors);

        // Act
        var result = await _fileTransformer.TransformFileAsync(_testStream, _testMeshFile);

        // Assert
        Assert.Equal(validationErrors, result);
        _fileParserMock.Verify(p => p.Parse(_testStream), Times.Once);
        _validationRunnerMock.Verify(v => v.Validate(parsedFile), Times.Once);
        _stagingPersisterMock.Verify(s => s.WriteStagedData(It.IsAny<ParsedFile>(), It.IsAny<MeshFile>()), Times.Never);
        _loggerMock.VerifyNoLogs(LogLevel.Error);
    }

    [Fact]
    public async Task TransformFileAsync_FileParsingExceptionThrown_ReturnsFileValidationError()
    {
        // Arrange
        var fileParsingException = new FileParsingException(ErrorCodes.UnknownRecordTypeIdentifier, "Unknown record type identifier 'INVALID_TYPE'");

        _fileParserMock.Setup(p => p.Parse(_testStream)).Throws(fileParsingException);

        // Act
        var result = await _fileTransformer.TransformFileAsync(_testStream, _testMeshFile);

        // Assert
        Assert.Single(result);
        var validationError = result[0];
        Assert.Equal(ErrorCodes.UnknownRecordTypeIdentifier, validationError.Code);
        Assert.Equal("Unknown record type identifier 'INVALID_TYPE'", validationError.Error);
        Assert.Equal(ValidationErrorScope.File, validationError.Scope);

        _fileParserMock.Verify(p => p.Parse(_testStream), Times.Once);
        _validationRunnerMock.Verify(v => v.Validate(It.IsAny<ParsedFile>()), Times.Never);
        _stagingPersisterMock.Verify(s => s.WriteStagedData(It.IsAny<ParsedFile>(), It.IsAny<MeshFile>()), Times.Never);

        _loggerMock.VerifyLogger(LogLevel.Error,
            $"File parsing failed with validation error. Code: {ErrorCodes.UnknownRecordTypeIdentifier}, Message: Unknown record type identifier 'INVALID_TYPE'");
    }

    [Fact]
    public async Task TransformFileAsync_UnexpectedExceptionThrown_ReturnsSystemValidationError()
    {
        // Arrange
        var unexpectedException = new InvalidOperationException("Something went wrong");
        _fileParserMock.Setup(p => p.Parse(_testStream)).Throws(unexpectedException);

        // Act
        var result = await _fileTransformer.TransformFileAsync(_testStream, _testMeshFile);

        // Assert
        Assert.Single(result);
        var validationError = result[0];
        Assert.Equal(ErrorCodes.UnableToParseFile, validationError.Code);
        Assert.Equal("Unable to parse file", validationError.Error);
        Assert.Equal(ValidationErrorScope.File, validationError.Scope);

        _fileParserMock.Verify(p => p.Parse(_testStream), Times.Once);
        _validationRunnerMock.Verify(v => v.Validate(It.IsAny<ParsedFile>()), Times.Never);
        _stagingPersisterMock.Verify(s => s.WriteStagedData(It.IsAny<ParsedFile>(), It.IsAny<MeshFile>()), Times.Never);

        _loggerMock.VerifyLogger(LogLevel.Error,
            $"System error occurred while parsing NBSS appointment file. File: {_testMeshFile.FileId}",
            ex => ex == unexpectedException);
    }

    [Fact]
    public async Task TransformFileAsync_UnexpectedExceptionWithNullMetaData_LogsUnknownFileName()
    {
        // Arrange
        var unexpectedException = new InvalidOperationException("Something went wrong");
        _fileParserMock.Setup(p => p.Parse(_testStream)).Throws(unexpectedException);

        // Act
        var result = await _fileTransformer.TransformFileAsync(_testStream, null);

        // Assert
        Assert.Single(result);
        var validationError = result[0];
        Assert.Equal(ErrorCodes.UnableToParseFile, validationError.Code);
        Assert.Equal("Unable to parse file", validationError.Error);
        Assert.Equal(ValidationErrorScope.File, validationError.Scope);

        _loggerMock.VerifyLogger(LogLevel.Error,
            "System error occurred while parsing NBSS appointment file. File: Unknown",
            ex => ex == unexpectedException);
    }

    [Fact]
    public async Task TransformFileAsync_ValidationRunnerThrowsException_ReturnsSystemValidationError()
    {
        // Arrange
        var validationException = new InvalidOperationException("Validation failed");

        _fileParserMock.Setup(p => p.Parse(_testStream)).Returns(parsedFile);
        _validationRunnerMock.Setup(v => v.Validate(parsedFile)).Throws(validationException);

        // Act
        var result = await _fileTransformer.TransformFileAsync(_testStream, _testMeshFile);

        // Assert
        Assert.Single(result);
        var validationError = result[0];
        Assert.Equal(ErrorCodes.UnableToParseFile, validationError.Code);
        Assert.Equal("Unable to parse file", validationError.Error);
        Assert.Equal(ValidationErrorScope.File, validationError.Scope);

        _fileParserMock.Verify(p => p.Parse(_testStream), Times.Once);
        _validationRunnerMock.Verify(v => v.Validate(parsedFile), Times.Once);
        _stagingPersisterMock.Verify(s => s.WriteStagedData(It.IsAny<ParsedFile>(), It.IsAny<MeshFile>()), Times.Never);

        _loggerMock.VerifyLogger(LogLevel.Error,
            $"System error occurred while parsing NBSS appointment file. File: {_testMeshFile.FileId}",
            ex => ex == validationException);
    }

    [Fact]
    public async Task TransformFileAsync_StagingPersisterThrowsException_ReturnsSystemValidationError()
    {
        // Arrange
        var validationErrors = new List<ValidationError>();
        var persistException = new InvalidOperationException("Database error");

        _fileParserMock.Setup(p => p.Parse(_testStream)).Returns(parsedFile);
        _validationRunnerMock.Setup(v => v.Validate(parsedFile)).Returns(validationErrors);
        _stagingPersisterMock.Setup(s => s.WriteStagedData(parsedFile, _testMeshFile)).ThrowsAsync(persistException);

        // Act
        var result = await _fileTransformer.TransformFileAsync(_testStream, _testMeshFile);

        // Assert
        Assert.Single(result);
        var validationError = result[0];
        Assert.Equal(ErrorCodes.UnableToParseFile, validationError.Code);
        Assert.Equal("Unable to parse file", validationError.Error);
        Assert.Equal(ValidationErrorScope.File, validationError.Scope);

        _fileParserMock.Verify(p => p.Parse(_testStream), Times.Once);
        _validationRunnerMock.Verify(v => v.Validate(parsedFile), Times.Once);
        _stagingPersisterMock.Verify(s => s.WriteStagedData(parsedFile, _testMeshFile), Times.Once);

        _loggerMock.VerifyLogger(LogLevel.Error,
            $"System error occurred while parsing NBSS appointment file. File: {_testMeshFile.FileId}",
            ex => ex == persistException);
    }

    [Theory]
    [InlineData(ErrorCodes.MissingFieldHeadings, "Field headings are missing")]
    [InlineData(ErrorCodes.UnknownRecordTypeIdentifier, "Unknown record type 'INVALID'")]
    [InlineData("CUSTOM001", "Custom validation error")]
    public async Task TransformFileAsync_DifferentFileParsingExceptions_ReturnsCorrectValidationErrors(string errorCode, string errorMessage)
    {
        // Arrange
        var fileParsingException = new FileParsingException(errorCode, errorMessage);
        _fileParserMock.Setup(p => p.Parse(_testStream)).Throws(fileParsingException);

        // Act
        var result = await _fileTransformer.TransformFileAsync(_testStream, _testMeshFile);

        // Assert
        Assert.Single(result);
        var validationError = result[0];
        Assert.Equal(errorCode, validationError.Code);
        Assert.Equal(errorMessage, validationError.Error);
        Assert.Equal(ValidationErrorScope.File, validationError.Scope);

        _loggerMock.VerifyLogger(LogLevel.Error,
            $"File parsing failed with validation error. Code: {errorCode}, Message: {errorMessage}");
    }
}
