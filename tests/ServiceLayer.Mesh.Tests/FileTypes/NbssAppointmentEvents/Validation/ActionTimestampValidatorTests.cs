using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class ActionTimestampValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_ActionTimestampMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Action Timestamp"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Action Timestamp",
            "Action Timestamp is missing",
            ErrorCodes.MissingActionTimestamp
        );
    }

    [Theory]
    [InlineData("20250631-183156")]     // too many days in June
    [InlineData("202S0630-183156")]     // invalid character
    [InlineData("20250630-1831")]       // too short
    [InlineData("20250630T1831")]       // unexpected separator
    [InlineData("250630-183156")]       // too short, ddMMyy
    [InlineData("20250630-1456")]       // No seconds
    [InlineData("20250630-18:31:56")]   // unexpected separators
    [InlineData("20250229-183156")]     // Not a leap year
    public void Validate_ActionTimestampInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Action Timestamp"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Action Timestamp",
            "Action Timestamp is in an invalid format",
            ErrorCodes.InvalidActionTimestamp
            );
    }

    [Theory]
    [InlineData("20250529-163243")]
    [InlineData("20240229-163243")]
    [InlineData("20250731-163243")]
    [InlineData("19990806-235959")]
    [InlineData("20561212-000000")]
    public void Validate_ActionTimestampValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Action Timestamp"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
