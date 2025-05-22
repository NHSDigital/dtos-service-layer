using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ActionValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_ActionMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Action"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Action",
            "Action is missing",
            ErrorCodes.MissingAction
        );
    }

    [Theory]
    [InlineData("A")]       // invalid character
    [InlineData("D")]       // invalid character
    [InlineData("$")]       // invalid character
    [InlineData("b")]       // lowercase
    [InlineData("")]        // Blank
    [InlineData(" ")]       // Whitespace
    [InlineData("BC")]      // Too many characters
    [InlineData("BCU")]     // Too many characters
    public void Validate_ActionInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Action"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Action",
            "Action is in an invalid format",
            ErrorCodes.InvalidAction
            );
    }

    [Theory]
    [InlineData("B")]
    [InlineData("C")]
    [InlineData("U")]
    public void Validate_ActionValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Action"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
