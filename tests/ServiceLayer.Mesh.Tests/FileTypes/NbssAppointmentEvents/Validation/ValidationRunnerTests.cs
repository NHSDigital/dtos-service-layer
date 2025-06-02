using Moq;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ValidationRunnerTests
{
    private readonly Mock<IValidationRunnerConfiguration> _configurationMock = new();

    public ValidationRunnerTests()
    {
        _configurationMock.Setup(c => c.MaximumValidationErrors).Returns(100);
    }

    [Fact]
    public void Validate_FileAndRecordValidatorsReturnErrors_ReturnsAllErrors()
    {
        // Arrange
        var file = new ParsedFile
        {
            DataRecords = [new FileDataRecord(), new FileDataRecord()]
        };

        var fileValidationError1 = BuildValidationError(ValidationErrorScope.File);
        var fileValidationError2 = BuildValidationError(ValidationErrorScope.File);
        var fileValidationError3 = BuildValidationError(ValidationErrorScope.File);
        var recordValidationError1 = BuildValidationError(ValidationErrorScope.Record);
        var recordValidationError2 = BuildValidationError(ValidationErrorScope.Record);
        var recordValidationError3 = BuildValidationError(ValidationErrorScope.Record);

        var expectedErrors = new List<ValidationError>
        {
            fileValidationError1, fileValidationError2, fileValidationError3,
            recordValidationError1, recordValidationError2, recordValidationError3,
            recordValidationError1, recordValidationError2, recordValidationError3,
        };

        var fileValidator1 = new Mock<IFileValidator>();
        fileValidator1
            .Setup(v => v.Validate(file))
            .Returns([fileValidationError1, fileValidationError2]);

        var fileValidator2 = new Mock<IFileValidator>();
        fileValidator2
            .Setup(v => v.Validate(file))
            .Returns([fileValidationError3]);

        var recordValidator1 = new Mock<IRecordValidator>();
        recordValidator1
            .Setup(v => v.Validate(It.IsAny<FileDataRecord>()))
            .Returns([recordValidationError1]);

        var recordValidator2 = new Mock<IRecordValidator>();
        recordValidator2
            .Setup(v => v.Validate(It.IsAny<FileDataRecord>()))
            .Returns([recordValidationError2, recordValidationError3]);

        var runner = new ValidationRunner(
            _configurationMock.Object,
            [fileValidator1.Object, fileValidator2.Object],
            [recordValidator1.Object, recordValidator2.Object]);

        // Act
        var results = runner.Validate(file);

        // Assert
        Assert.Equal(expectedErrors, results, new ValidationErrorComparer());
    }

    [Fact]
    public void Validate_OnlyRecordValidatorsReturnErrors_ReturnsAllErrors()
    {
        // Arrange
        var file = new ParsedFile
        {
            DataRecords = [new FileDataRecord(), new FileDataRecord()]
        };

        var recordValidationError1 = BuildValidationError(ValidationErrorScope.Record);
        var recordValidationError2 = BuildValidationError(ValidationErrorScope.Record);
        var recordValidationError3 = BuildValidationError(ValidationErrorScope.Record);

        var expectedErrors = new List<ValidationError>
        {
            recordValidationError1, recordValidationError2, recordValidationError3,
            recordValidationError1, recordValidationError2, recordValidationError3,
        };

        var fileValidator1 = new Mock<IFileValidator>();
        fileValidator1
            .Setup(v => v.Validate(file))
            .Returns([]);

        var fileValidator2 = new Mock<IFileValidator>();
        fileValidator2
            .Setup(v => v.Validate(file))
            .Returns([]);

        var recordValidator1 = new Mock<IRecordValidator>();
        recordValidator1
            .Setup(v => v.Validate(It.IsAny<FileDataRecord>()))
            .Returns([recordValidationError1]);

        var recordValidator2 = new Mock<IRecordValidator>();
        recordValidator2
            .Setup(v => v.Validate(It.IsAny<FileDataRecord>()))
            .Returns([recordValidationError2, recordValidationError3]);

        var runner = new ValidationRunner(
            _configurationMock.Object,
            [fileValidator1.Object, fileValidator2.Object],
            [recordValidator1.Object, recordValidator2.Object]);

        // Act
        var results = runner.Validate(file);

        // Assert
        Assert.Equal(expectedErrors, results, new ValidationErrorComparer());
    }

    [Fact]
    public void Validate_OnlyFileValidatorsReturnErrors_ReturnsAllErrors()
    {
        // Arrange
        var file = new ParsedFile
        {
            DataRecords = [new FileDataRecord(), new FileDataRecord()]
        };

        var fileValidationError1 = BuildValidationError(ValidationErrorScope.File);
        var fileValidationError2 = BuildValidationError(ValidationErrorScope.File);
        var fileValidationError3 = BuildValidationError(ValidationErrorScope.File);

        var expectedErrors = new List<ValidationError>
        {
            fileValidationError1, fileValidationError2, fileValidationError3
        };

        var fileValidator1 = new Mock<IFileValidator>();
        fileValidator1
            .Setup(v => v.Validate(file))
            .Returns([fileValidationError1, fileValidationError2]);

        var fileValidator2 = new Mock<IFileValidator>();
        fileValidator2
            .Setup(v => v.Validate(file))
            .Returns([fileValidationError3]);

        var recordValidator1 = new Mock<IRecordValidator>();
        recordValidator1
            .Setup(v => v.Validate(It.IsAny<FileDataRecord>()))
            .Returns([]);

        var recordValidator2 = new Mock<IRecordValidator>();
        recordValidator2
            .Setup(v => v.Validate(It.IsAny<FileDataRecord>()))
            .Returns([]);

        var runner = new ValidationRunner(
            _configurationMock.Object,
            [fileValidator1.Object, fileValidator2.Object],
            [recordValidator1.Object, recordValidator2.Object]);

        // Act
        var results = runner.Validate(file);

        // Assert
        Assert.Equal(expectedErrors, results, new ValidationErrorComparer());
    }

    [Fact]
    public void Validate_FileAndRecordValidatorsReturnNoErrors_ReturnsNoErrors()
    {
        // Arrange
        var file = new ParsedFile
        {
            DataRecords = [new FileDataRecord(), new FileDataRecord()]
        };

        var fileValidator1 = new Mock<IFileValidator>();
        fileValidator1
            .Setup(v => v.Validate(file))
            .Returns([]);

        var fileValidator2 = new Mock<IFileValidator>();
        fileValidator2
            .Setup(v => v.Validate(file))
            .Returns([]);

        var recordValidator1 = new Mock<IRecordValidator>();
        recordValidator1
            .Setup(v => v.Validate(It.IsAny<FileDataRecord>()))
            .Returns([]);

        var recordValidator2 = new Mock<IRecordValidator>();
        recordValidator2
            .Setup(v => v.Validate(It.IsAny<FileDataRecord>()))
            .Returns([]);

        var runner = new ValidationRunner(
            _configurationMock.Object,
            [fileValidator1.Object, fileValidator2.Object],
            [recordValidator1.Object, recordValidator2.Object]);

        // Act
        var results = runner.Validate(file);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(19, 10, 10, true, 20)]
    [InlineData(20, 10, 10, true, 21)]
    [InlineData(21, 10, 10, false, 20)]
    [InlineData(100, 49, 50, false, 99)]
    [InlineData(100, 100, 0, true, 101)]
    [InlineData(100, 0, 100, true, 101)]
    [InlineData(100, 100, 100, true, 101)]
    [InlineData(100, 200, 200, true, 101)]
    public void Validate_TooManyFileValidationErrors_ReturnsFirstErrorsPlusIndicationOfEarlyTermination(
        int maximumErrorCount, int validator1ErrorCount, int validator2ErrorCount, bool expectAborted,
        int expectedErrorCount)
    {
        // Arrange
        var file = new ParsedFile
        {
            DataRecords = [new FileDataRecord(), new FileDataRecord()]
        };

        var fileValidator1 = new Mock<IFileValidator>();
        fileValidator1
            .Setup(v => v.Validate(file))
            .Returns(BuildValidationErrors(ValidationErrorScope.File, validator1ErrorCount));
        var fileValidator2 = new Mock<IFileValidator>();
        fileValidator2
            .Setup(v => v.Validate(file))
            .Returns(BuildValidationErrors(ValidationErrorScope.File, validator2ErrorCount));

        _configurationMock.Setup(c => c.MaximumValidationErrors)
            .Returns(maximumErrorCount);

        var runner = new ValidationRunner(
            _configurationMock.Object,
            [fileValidator1.Object, fileValidator2.Object], []);

        // Act
        var results = runner.Validate(file);

        // Assert
        Assert.Equal(expectedErrorCount, results.Count);
        if (expectAborted)
        {
            AssertContainsValidationAbortedError(results, maximumErrorCount);
        }
        else
        {
            AssertDoesNotContainValidationAbortedError(results, maximumErrorCount);
        }
    }

    [Theory]
    [InlineData(39, 10, 10, true, 40)]
    [InlineData(40, 10, 10, true, 41)]
    [InlineData(41, 10, 10, false, 40)]
    [InlineData(100, 24, 25, false, 98)]
    [InlineData(100, 25, 25, true, 101)]
    [InlineData(100, 50, 0, true, 101)]
    [InlineData(100, 0, 50, true, 101)]
    [InlineData(100, 50, 50, true, 101)]
    [InlineData(100, 100, 100, true, 101)]
    public void Validate_TooManyRecordValidatorErrors_ReturnsFirstErrorsPlusIndicationOfEarlyTermination(
        int maximumErrorCount, int validator1ErrorCount, int validator2ErrorCount, bool expectAborted,
        int expectedErrorCount)
    {
        // Arrange
        var file = new ParsedFile
        {
            DataRecords = [new FileDataRecord(), new FileDataRecord()]
        };

        var recordValidator1 = new Mock<IRecordValidator>();
        recordValidator1
            .Setup(v => v.Validate(It.IsAny<FileDataRecord>()))
            .Returns(BuildValidationErrors(ValidationErrorScope.File, validator1ErrorCount));
        var recordValidator2 = new Mock<IRecordValidator>();
        recordValidator2
            .Setup(v => v.Validate(It.IsAny<FileDataRecord>()))
            .Returns(BuildValidationErrors(ValidationErrorScope.File, validator2ErrorCount));

        _configurationMock.Setup(c => c.MaximumValidationErrors)
            .Returns(maximumErrorCount);

        var runner = new ValidationRunner(_configurationMock.Object,
            [], [recordValidator1.Object, recordValidator2.Object]);

        // Act
        var results = runner.Validate(file);

        // Assert
        Assert.Equal(expectedErrorCount, results.Count);
        if (expectAborted)
        {
            AssertContainsValidationAbortedError(results, maximumErrorCount);
        }
        else
        {
            AssertDoesNotContainValidationAbortedError(results, maximumErrorCount);
        }
    }

    [Theory]
    [InlineData(29, 10, 10, true, 30)]
    [InlineData(30, 10, 10, true, 31)]
    [InlineData(31, 10, 10, false, 30)]
    [InlineData(100, 49, 25, false, 99)]
    [InlineData(100, 50, 25, true, 101)]
    [InlineData(100, 100, 0, true, 101)]
    [InlineData(100, 0, 50, true, 101)]
    [InlineData(100, 100, 50, true, 101)]
    [InlineData(100, 200, 100, true, 101)]
    public void Validate_TooManyValidatorErrors_ReturnsFirstErrorsPlusIndicationOfEarlyTermination(
        int maximumErrorCount, int fileValidatorErrorCount, int recordValidatorErrorCount, bool expectAborted,
        int expectedErrorCount)
    {
        // Arrange
        var file = new ParsedFile
        {
            DataRecords = [new FileDataRecord(), new FileDataRecord()]
        };

        var fileValidator = new Mock<IFileValidator>();
        fileValidator
            .Setup(v => v.Validate(file))
            .Returns(BuildValidationErrors(ValidationErrorScope.File, fileValidatorErrorCount));
        var recordValidator = new Mock<IRecordValidator>();
        recordValidator
            .Setup(v => v.Validate(It.IsAny<FileDataRecord>()))
            .Returns(BuildValidationErrors(ValidationErrorScope.File, recordValidatorErrorCount));

        _configurationMock.Setup(c => c.MaximumValidationErrors)
            .Returns(maximumErrorCount);

        var runner = new ValidationRunner(_configurationMock.Object,
            [fileValidator.Object], [recordValidator.Object]);

        // Act
        var results = runner.Validate(file);

        // Assert
        Assert.Equal(expectedErrorCount, results.Count);
        if (expectAborted)
        {
            AssertContainsValidationAbortedError(results, maximumErrorCount);
        }
        else
        {
            AssertDoesNotContainValidationAbortedError(results, maximumErrorCount);
        }
    }

    private static IEnumerable<ValidationError> BuildValidationErrors(ValidationErrorScope scope, int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => BuildValidationError(scope));
    }

    private static ValidationError BuildValidationError(ValidationErrorScope scope)
    {
        var validationError = new ValidationError
        {
            Code = Guid.NewGuid().ToString(),
            Error = Guid.NewGuid().ToString(),
            Field = Guid.NewGuid().ToString(),
            Scope = scope
        };

        if (scope == ValidationErrorScope.Record)
        {
            validationError.RowNumber = 1;
        }

        return validationError;
    }

    private static void AssertContainsValidationAbortedError(IList<ValidationError> errors, int maximumErrors)
    {
        Assert.Contains(errors, BuildValidationAbortedPredicate(maximumErrors));
    }

    private static void AssertDoesNotContainValidationAbortedError(IList<ValidationError> errors, int maximumErrors)
    {
        Assert.DoesNotContain(errors, BuildValidationAbortedPredicate(maximumErrors));
    }

    private static Predicate<ValidationError> BuildValidationAbortedPredicate(int maximumErrors)
    {
        var errorMessage = $"Validation aborted after {maximumErrors} errors encountered";

        return e =>
            e.Code == ErrorCodes.ValidationAborted &&
            e.Error == errorMessage &&
            e.Scope == ValidationErrorScope.File &&
            e.Field is null &&
            e.RowNumber is null;
    }
}
