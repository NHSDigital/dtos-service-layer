using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class StatusValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_StatusMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Status"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Status",
            "Status is missing",
            ErrorCodes.MissingStatus
        );
    }

    [Theory]
    [InlineData("E")]       // invalid character
    [InlineData("F")]       // invalid character
    [InlineData("$")]       // invalid character
    [InlineData("b")]       // lowercase
    [InlineData("")]        // Blank
    [InlineData(" ")]       // Whitespace
    [InlineData("AB")]      // Too many characters
    [InlineData("BCD")]     // Too many characters
    public void Validate_StatusInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Status"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Status",
            "Status is in an invalid format",
            ErrorCodes.InvalidStatus
            );
    }

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("C")]
    [InlineData("D")]
    public void Validate_StatusValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Status"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
