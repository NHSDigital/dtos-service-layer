using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ApptTimeValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_ApptTimeMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Appt Time"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Appt Time",
            "Appt Time is missing",
            ErrorCodes.MissingApptTime
        );
    }

    [Theory]
    [InlineData("2407")]              // too many hours
    [InlineData("1960")]              // too many minutes
    [InlineData("842")]               // too short
    [InlineData("10435")]             // too long
    [InlineData("193S")]              // invalid characters
    public void Validate_ApptTimeInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Appt Time"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Appt Time",
            "Appt Time is in an invalid format",
            ErrorCodes.InvalidApptTime
            );
    }

    [Theory]
    [InlineData("0000")]
    [InlineData("2359")]
    [InlineData("0001")]
    [InlineData("2358")]
    [InlineData("1200")]
    [InlineData("1300")]
    public void Validate_ApptTimeValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Appt Time"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
