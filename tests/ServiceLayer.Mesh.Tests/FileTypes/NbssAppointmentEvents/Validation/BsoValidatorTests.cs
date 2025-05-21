using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class BsoValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_BSOMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("BSO"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "BSO",
            "BSO is missing or empty",
            ErrorCodes.MissingBso
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_BsoBlank_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["BSO"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "BSO",
            "BSO is missing or empty",
            ErrorCodes.MissingBso
        );
    }

    [Theory]
    [InlineData("ABCD")]    // 4 characters
    [InlineData("ABCDE")]   // 5 characters
    public void Validate_BsoTooLong_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["BSO"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "BSO",
            "BSO exceeds maximum length of 3",
            ErrorCodes.InvalidBso
        );
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("RD5")]
    public void Validate_BSOValid_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["BSO"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        Assert.Empty(validationErrors);
    }
}
