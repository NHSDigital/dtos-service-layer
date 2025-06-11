using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ExtractIdValidatorTests : ValidationTestBase
{
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
}
