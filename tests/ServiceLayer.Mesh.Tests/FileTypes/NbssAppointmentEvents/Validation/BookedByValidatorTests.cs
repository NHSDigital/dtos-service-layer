using ServiceLayer.Mesh.FileTypes.NbssAppointmentEvents.Validation;

namespace ServiceLayer.Mesh.Tests.FileTypes.NbssAppointmentEvents.Validation;

public class BookedByValidatorTests : ValidationTestBase
{
    [Fact]
    public void Validate_BookedByMissing_ReturnsValidationError()
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields.Remove("Booked By"));

        // Act
        var validationErrors = Validate(file);

        // Assert
        validationErrors.ShouldContainValidationError(
            "Booked By",
            "Booked By is missing",
            ErrorCodes.MissingBookedBy
        );
    }

    [Theory]
    [InlineData("A")]       // invalid character
    [InlineData("B")]       // invalid character
    [InlineData("$")]       // invalid character
    [InlineData("c")]       // lowercase
    [InlineData("")]        // Blank
    [InlineData(" ")]       // Whitespace
    [InlineData("CH")]      // Too many characters
    public void Validate_BookedByInvalidFormat_ReturnsValidationError(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Booked By"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        validationErrors.ShouldContainValidationError(
            "Booked By",
            "Booked By is in an invalid format",
            ErrorCodes.InvalidBookedBy
            );
    }

    [Theory]
    [InlineData("C")]
    [InlineData("H")]
    public void Validate_BookedByValidFormat_NoValidationErrorsReturned(string value)
    {
        // Arrange
        var file = ParsedFileWithModifiedRecord(r => r.Fields["Booked By"] = value);

        // Act
        var validationErrors = Validate(file).ToList();

        // Assert
        Assert.Empty(validationErrors);
    }
}
