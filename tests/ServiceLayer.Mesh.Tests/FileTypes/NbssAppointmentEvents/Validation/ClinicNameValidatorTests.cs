using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ClinicNameValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_ClinicNameMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Clinic Name"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Clinic Name",
            "Clinic Name is missing",
            ErrorCodes.MissingClinicName
        );
    }

    [Theory]
    [InlineData("12345678901234567890123456789012345678901")]             // 41 characters
    [InlineData("12345678901234567890123456789012345678901234567890")]    // 50 characters
    public void Validate_ClinicNameTooLong_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Clinic Name"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Clinic Name",
            "Clinic Name exceeds maximum length of 40",
            ErrorCodes.InvalidClinicName
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567890123456789012345678901234567890")]
    [InlineData("Breast Care Unit")]
    public void Validate_ClinicNameValid_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Clinic Name"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
