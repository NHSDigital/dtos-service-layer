using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ScreenOrAssesValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_ScreenOrAssesMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Screen or Asses"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Screen or Asses",
            "Screen or Asses is missing",
            ErrorCodes.MissingScreenOrAsses
        );
    }

    [Theory]
    [InlineData("B")]       // invalid character
    [InlineData("C")]       // invalid character
    [InlineData("$")]       // invalid character
    [InlineData("a")]       // lowercase
    [InlineData("")]        // Blank
    [InlineData(" ")]       // Whitespace
    [InlineData("AS")]      // Too many characters
    public void Validate_ScreenOrAssesInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Screen or Asses"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Screen or Asses",
            "Screen or Asses is in an invalid format",
            ErrorCodes.InvalidScreenOrAsses
            );
    }

    [Theory]
    [InlineData("A")]
    [InlineData("S")]
    public void Validate_ScreenOrAssesValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Screen or Asses"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
