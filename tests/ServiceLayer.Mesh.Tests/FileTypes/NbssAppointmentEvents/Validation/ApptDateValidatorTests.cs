using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ApptDateValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_ApptDateMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Appt Date"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Appt Date",
            "Appt Date is missing",
            ErrorCodes.MissingApptDate
        );
    }

    [Theory]
    [InlineData("20250631")]              // too many days in June
    [InlineData("202S0630")]              // invalid character
    [InlineData("202506")]                // too short
    [InlineData("30062025")]              // ddMMyyyy and not valid as yyyyMMdd
    [InlineData("250630")]                // too short, ddMMyy
    [InlineData("20250630-145621")]       // Includes time
    [InlineData("20250229")]              // Not a leap year
    public void Validate_ApptDateInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Appt Date"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Appt Date",
            "Appt Date is in an invalid format",
            ErrorCodes.InvalidApptDate
            );
    }

    [Theory]
    [InlineData("20250101")]
    [InlineData("20250228")]
    [InlineData("20250331")]
    [InlineData("20251231")]
    [InlineData("20240229")]
    [InlineData("19990331")]
    public void Validate_ApptDateValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Appt Date"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
