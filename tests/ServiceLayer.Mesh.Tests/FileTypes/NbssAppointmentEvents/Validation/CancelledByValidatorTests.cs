using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class CancelledByValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_CancelledByMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Cancelled By"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Cancelled By",
            "Cancelled By is missing",
            ErrorCodes.MissingCancelledBy
        );
    }

    [Theory]
    [InlineData("A")]       // invalid character
    [InlineData("D")]       // invalid character
    [InlineData("  ")]      // Too many characters
    [InlineData("CH")]      // Too many characters
    public void Validate_CancelledByInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Cancelled By"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Cancelled By",
            "Cancelled By is in an invalid format",
            ErrorCodes.InvalidCancelledBy
            );
    }

    [Theory]
    [InlineData("C")]
    [InlineData("H")]
    [InlineData(" ")]
    [InlineData("")]
    public void Validate_CancelledByValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Cancelled By"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
