using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class LocationValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_LocationMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Location"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Location",
            "Location is missing or empty",
            ErrorCodes.MissingLocation
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_LocationBlank_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Location"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Location",
            "Location is missing or empty",
            ErrorCodes.MissingLocation
        );
    }

    [Theory]
    [InlineData("123456")]        // 6 characters
    [InlineData("1234567890")]    // 10 characters
    public void Validate_LocationTooLong_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Location"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Location",
            "Location exceeds maximum length of 5",
            ErrorCodes.InvalidLocation
        );
    }

    [Theory]
    [InlineData("KIN10")]
    [InlineData("BU")]
    public void Validate_LocationValid_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Location"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        Assert.Empty(validationErrors);
    }
}
