using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ScreenApptNumValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_ScreenApptNumMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Screen Appt num"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Screen Appt num",
            "Screen Appt num is missing",
            ErrorCodes.MissingScreenApptNum
        );
    }

    [Theory]
    [InlineData("A")]       // invalid character
    [InlineData("a")]       // lowercase
    [InlineData(" ")]       // Whitespace
    [InlineData("12")]      // Too many characters
    [InlineData("0")]       // Zero
    public void Validate_ScreenApptNumInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Screen Appt num"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Screen Appt num",
            "Screen Appt num is in an invalid format",
            ErrorCodes.InvalidScreenApptNum
            );
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("5")]
    [InlineData("6")]
    [InlineData("7")]
    [InlineData("8")]
    [InlineData("9")]
    [InlineData("")]
    public void Validate_ScreenApptNumValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Screen Appt num"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
