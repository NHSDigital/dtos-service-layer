using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class HoldingClinicValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_HoldingClinicMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Holding Clinic"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Holding Clinic",
            "Holding Clinic is missing",
            ErrorCodes.MissingHoldingClinic
        );
    }

    [Theory]
    [InlineData("A")]       // invalid character
    [InlineData("D")]       // invalid character
    [InlineData("  ")]      // Too many characters
    [InlineData("YN")]      // Too many characters
    [InlineData("Y ")]      // Too many characters
    public void Validate_HoldingClinicInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Holding Clinic"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Holding Clinic",
            "Holding Clinic is in an invalid format",
            ErrorCodes.InvalidHoldingClinic
            );
    }

    [Theory]
    [InlineData("Y")]
    [InlineData("N")]
    [InlineData(" ")]
    [InlineData("")]
    public void Validate_HoldingClinicValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Holding Clinic"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
