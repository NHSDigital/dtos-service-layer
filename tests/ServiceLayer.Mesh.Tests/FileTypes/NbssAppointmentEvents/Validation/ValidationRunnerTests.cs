using Moq;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Models;
using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ValidationRunnerTests
{
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
            [fileValidator1.Object, fileValidator2.Object],
            [recordValidator1.Object, recordValidator2.Object]);

        // Act
        var results = runner.Validate(file);

        // Assert
        Assert.Empty(results);
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
}
