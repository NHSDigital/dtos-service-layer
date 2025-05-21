using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class AttendedNotScrValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_AttendedNotScrMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Attended Not Scr"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Attended Not Scr",
            "Attended Not Scr is missing",
            ErrorCodes.MissingAttendedNotScr
        );
    }

    [Theory]
    [InlineData("A")]       // invalid character
    [InlineData("D")]       // invalid character
    [InlineData("  ")]      // Too many characters
    [InlineData("YN")]      // Too many characters
    [InlineData("Y ")]      // Too many characters
    public void Validate_AttendedNotScrInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Attended Not Scr"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Attended Not Scr",
            "Attended Not Scr is in an invalid format",
            ErrorCodes.InvalidAttendedNotScr
            );
    }

    [Theory]
    [InlineData("Y")]
    [InlineData("N")]
    [InlineData(" ")]
    [InlineData("")]
    public void Validate_AttendedNotScrValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Attended Not Scr"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
