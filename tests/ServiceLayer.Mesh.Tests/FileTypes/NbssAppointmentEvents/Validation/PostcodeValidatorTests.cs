using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class PostcodeValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_PostcodeMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Postcode"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Postcode",
            "Postcode is missing",
            ErrorCodes.MissingPostcode
        );
    }

    [Theory]
    [InlineData("LS25 6LGG")]   // 9 characters
    [InlineData("YO31 88RQY")]  // 10 characters
    public void Validate_PostcodeTooLong_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Postcode"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Postcode",
            "Postcode exceeds maximum length of 8",
            ErrorCodes.InvalidPostcode
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("S81 8SH")]
    [InlineData("YO31 8RQ")]
    public void Validate_PostcodeValid_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Postcode"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
