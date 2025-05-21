using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class AppointmentIdValidationTests : ValidationTestBase
{
    [Fact]
    public void Validate_AppointmentIdMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Appointment ID"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Appointment ID",
            "Appointment ID is missing or empty",
            ErrorCodes.MissingAppointmentId
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_AppointmentIdBlank_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Appointment ID"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Appointment ID",
            "Appointment ID is missing or empty",
            ErrorCodes.MissingAppointmentId
        );
    }

    [Theory]
    [InlineData("1234567890123456789012345678")]                // 28 characters
    [InlineData("1234567890123456789012345678901234567890")]    // 40 characters
    public void Validate_AppointmentIdTooLong_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Appointment ID"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldBeSingleValidationError(
            "Appointment ID",
            "Appointment ID exceeds maximum length of 27",
            ErrorCodes.InvalidAppointmentId
        );
    }

    [Theory]
    [InlineData("AS003-67240-RA1-DN-T1315-1")]
    [InlineData("AS003-67240-RA1-DN-T1045-01")]
    public void Validate_AppointmentIdValid_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Appointment ID"] = value);

        // Act
        var validationErrors = Validate(file);

        // Assert
        Assert.Empty(validationErrors);
    }
}
