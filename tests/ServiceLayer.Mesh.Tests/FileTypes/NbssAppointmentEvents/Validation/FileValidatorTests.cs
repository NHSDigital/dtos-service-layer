using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class FileValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_HeaderMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ValidParsedFile;
        file.FileHeader = null;

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            null,
            "Header is missing",
            ErrorCodes.MissingHeader,
            ValidationErrorScope.File
            );
    }

    [Fact]
    public void Validate_TrailerMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ValidParsedFile;
        file.FileTrailer = null;

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            null,
            "Trailer is missing",
            ErrorCodes.MissingTrailer,
            ValidationErrorScope.File
        );
    }

    [Fact]
    public void Validate_HeaderExtractIdMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ValidParsedFile;
        file.FileHeader!.ExtractId = null;

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Extract ID",
            "Extract ID is missing",
            ErrorCodes.MissingExtractId,
            ValidationErrorScope.Header
        );
    }

    [Theory]
    [InlineData("1")]         // Missing leading zeroes
    [InlineData("100000000")] // Too large
    [InlineData("")]          // Blank
    [InlineData("asdf")]      // NaN
    public void Validate_HeaderExtractIdInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ValidParsedFile;
        file.FileHeader!.ExtractId = value;

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Extract ID",
            "Extract ID is in an invalid format",
            ErrorCodes.InvalidExtractId,
            ValidationErrorScope.Header
        );
    }

    [Theory]
    [InlineData("00000000")]
    [InlineData("00000001")]
    [InlineData("99999999")]
    public void Validate_HeaderExtractIdValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ValidParsedFile;
        file.FileHeader!.ExtractId = value;
        file.FileTrailer!.ExtractId = value;

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }

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
    [InlineData("00000108")]
    public void Validate_TrailerExtractIdMismatch_ReturnsValidationError(string? value)
    {
        // Arrange
        var file = ValidParsedFile;
        file.FileTrailer!.ExtractId = value;

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Extract ID",
            "Extract ID does not match value in header",
            ErrorCodes.InconsistentExtractId,
            ValidationErrorScope.Trailer
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
