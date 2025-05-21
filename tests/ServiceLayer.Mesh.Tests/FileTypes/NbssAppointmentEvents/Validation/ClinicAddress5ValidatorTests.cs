using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ClinicAddress5ValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_ClinicAddress5Missing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Clinic Address 5"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Clinic Address 5",
            "Clinic Address 5 is missing",
            ErrorCodes.MissingClinicAddress5
        );
    }

    [Theory]
    [InlineData("1234567890123456789012345678901")]             // 31 characters
    [InlineData("1234567890123456789012345678901234567890")]    // 40 characters
    public void Validate_ClinicAddress5TooLong_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Clinic Address 5"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Clinic Address 5",
            "Clinic Address 5 exceeds maximum length of 30",
            ErrorCodes.InvalidClinicAddress5
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456789012345678901234567890")]
    [InlineData("Milton Keynes")]
    public void Validate_ClinicAddress5Valid_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Clinic Address 5"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
