using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ClinicNameLetValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_ClinicNameLetMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Clinic Name (Let)"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Clinic Name (Let)",
            "Clinic Name (Let) is missing",
            ErrorCodes.MissingClinicNameLet
        );
    }

    [Theory]
    [InlineData("1234567890123456789012345678901")]             // 31 characters
    [InlineData("1234567890123456789012345678901234567890")]    // 40 characters
    public void Validate_ClinicNameLetTooLong_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Clinic Name (Let)"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Clinic Name (Let)",
            "Clinic Name (Let) exceeds maximum length of 30",
            ErrorCodes.InvalidClinicNameLet
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456789012345678901234567890")]
    [InlineData("Breast Care Unit")]
    public void Validate_ClinicNameLetValid_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Clinic Name (Let)"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
