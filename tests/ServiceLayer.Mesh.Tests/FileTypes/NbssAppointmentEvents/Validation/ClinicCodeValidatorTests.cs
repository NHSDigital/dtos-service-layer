using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ClinicCodeValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_ClinicCodeMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Clinic Code"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Clinic Code",
            "Clinic Code is missing or empty",
            ErrorCodes.MissingClinicCode
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ClinicCodeBlank_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Clinic Code"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Clinic Code",
            "Clinic Code is missing or empty",
            ErrorCodes.MissingClinicCode
        );
    }

    [Theory]
    [InlineData("BS0004")]    // 6 characters
    [InlineData("BSO0007")]   // 7 characters
    public void Validate_ClinicCodeTooLong_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Clinic Code"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Clinic Code",
            "Clinic Code exceeds maximum length of 5",
            ErrorCodes.InvalidClinicCode
        );
    }

    [Theory]
    [InlineData("BS003")]
    [InlineData("KI011")]
    [InlineData("E17")]
    public void Validate_ClinicCodeValid_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Clinic Code"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        Assert.Empty(validationErrors);
    }
}
