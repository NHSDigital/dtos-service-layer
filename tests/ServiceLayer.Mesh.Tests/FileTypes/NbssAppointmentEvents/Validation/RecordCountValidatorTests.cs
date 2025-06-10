using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class RecordCountValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_HeaderRecordCountMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ValidParsedFile;
        file.FileHeader!.RecordCount = null;

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Record count",
            "Record count is missing",
            ErrorCodes.MissingRecordCount,
            ValidationErrorScope.Header
        );
    }

    [Theory]
    [InlineData("1")]         // Missing leading zeroes
    [InlineData("000000")]    // All zeroes
    [InlineData("1000000")]   // Too large
    [InlineData("")]          // Blank
    [InlineData("asdf")]      // NaN
    public void Validate_HeaderRecordCountInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ValidParsedFile;
        file.FileHeader!.RecordCount = value;

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Record count",
            "Record count is in an invalid format",
            ErrorCodes.InvalidRecordCount,
            ValidationErrorScope.Header
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("000002")]
    [InlineData("000004")]
    public void Validate_TrailerRecordCountMismatch_ReturnsValidationError(string? value)
    {
        // Arrange
        var file = ValidParsedFile;
        file.FileTrailer!.RecordCount = value;

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Record count",
            "Record count does not match value in header",
            ErrorCodes.InconsistentRecordCount,
            ValidationErrorScope.Trailer
        );
    }

    [Fact]
    public void Validate_UnexpectedRecordCount_ReturnsValidationError()
    {
        // Arrange
        var file = ValidParsedFile;
        file.DataRecords.RemoveAt(0);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            null,
            "Record count does not match value in header and trailer",
            ErrorCodes.UnexpectedRecordCount,
            ValidationErrorScope.File
        );
    }
}
